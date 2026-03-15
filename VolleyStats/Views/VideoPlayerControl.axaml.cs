using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using LibVLCSharp.Shared;

namespace VolleyStats.Views
{
    public partial class VideoPlayerControl : UserControl
    {
        private MediaPlayer? _player;
        private volatile bool _detached;

        private const uint W = 1280, H = 720;
        private const int FrameBytes = (int)(W * H * 4);
        private readonly byte[] _buf = new byte[FrameBytes];
        private GCHandle _bufHandle;

        // Dummy buffer that stays pinned forever — Lock callbacks always have a safe target
        private static readonly byte[] DummyBuf = new byte[FrameBytes];
        private static readonly GCHandle DummyHandle = GCHandle.Alloc(DummyBuf, GCHandleType.Pinned);
        private WriteableBitmap? _bitmap;
        private int _uiReady = 1;

        private string? _pendingPath;
        private int? _pendingSeek;
        private volatile bool _pauseAfterSeek;
        private volatile int _skipFrames;

        // ── Player pool ─────────────────────────────────────────────────────
        private class PooledPlayer
        {
            public MediaPlayer Player = null!;
            public byte[] Buffer = null!;
            public GCHandle Handle;
            public bool Ready;
        }

        private Dictionary<string, PooledPlayer>? _pool;
        private PooledPlayer? _activePooled;
        private string? _activePooledPath;

        // Drawing state
        private bool _isPaintMode;
        private Polyline? _currentStroke;

        public static readonly StyledProperty<string?> VideoPathProperty =
            AvaloniaProperty.Register<VideoPlayerControl, string?>(nameof(VideoPath));

        public string? VideoPath
        {
            get => GetValue(VideoPathProperty);
            set => SetValue(VideoPathProperty, value);
        }

        public static readonly StyledProperty<bool> PauseOnLoadProperty =
            AvaloniaProperty.Register<VideoPlayerControl, bool>(nameof(PauseOnLoad), defaultValue: true);

        public bool PauseOnLoad
        {
            get => GetValue(PauseOnLoadProperty);
            set => SetValue(PauseOnLoadProperty, value);
        }

        public static readonly StyledProperty<int?> VideoSeekSecondsProperty =
            AvaloniaProperty.Register<VideoPlayerControl, int?>(nameof(VideoSeekSeconds));

        public int? VideoSeekSeconds
        {
            get => GetValue(VideoSeekSecondsProperty);
            set => SetValue(VideoSeekSecondsProperty, value);
        }

        public VideoPlayerControl()
        {
            InitializeComponent();
            AttachedToVisualTree += OnAttached;
            DetachedFromVisualTree += OnDetached;
        }

        // ── Lifecycle ────────────────────────────────────────────────────────

        private async void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            _detached = false;
            _bufHandle = GCHandle.Alloc(_buf, GCHandleType.Pinned);

            try
            {
                var completed = await Task.WhenAny(
                    Program.LibVlcInitTask,
                    Task.Delay(10_000));

                if (completed != Program.LibVlcInitTask)
                {
                    ShowStatus("Video engine timed out");
                    return;
                }
            }
            catch
            {
                ShowStatus("Video engine error");
                return;
            }

            if (_detached) return;

            var vlc = Program.LibVlc;
            if (vlc == null)
            {
                ShowStatus("Video engine unavailable");
                return;
            }

            try
            {
                _bitmap = new WriteableBitmap(
                    new PixelSize((int)W, (int)H), new Vector(96, 96),
                    PixelFormats.Bgra8888, AlphaFormat.Opaque);
                VideoImage.Source = _bitmap;

                _player = CreatePlayer(vlc, _buf, _bufHandle);
            }
            catch
            {
                ShowStatus("Video player error");
                return;
            }

            var path = _pendingPath ?? VideoPath;
            _pendingPath = null;
            if (path != null)
                Load(path);
        }

        private MediaPlayer CreatePlayer(LibVLC vlc, byte[] buf, GCHandle handle)
        {
            var player = new MediaPlayer(vlc);
            player.SetVideoCallbacks(
                (opaque, planes) =>
                {
                    // Always write a valid pointer — VLC will crash if planes is left empty
                    if (_detached)
                        Marshal.WriteIntPtr(planes, DummyHandle.AddrOfPinnedObject());
                    else
                        Marshal.WriteIntPtr(planes, handle.AddrOfPinnedObject());
                    return IntPtr.Zero;
                },
                null,
                (opaque, picture) => Display(opaque, picture));
            player.SetVideoFormat("RV32", W, H, W * 4);

            player.Playing += (_, _) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_detached) return;
                    HideStatus();
                    PlayPauseBtn.Content = "\u23f8";
                });
                if (_pendingSeek.HasValue)
                {
                    var shouldPause = _pauseAfterSeek;
                    _pauseAfterSeek = false;
                    var p = player;
                    Task.Run(async () =>
                    {
                        await Task.Delay(300);
                        if (p == null || _detached) return;
                        var seek = _pendingSeek;
                        if (!seek.HasValue) return;
                        _pendingSeek = null;
                        var seekMs = (long)seek.Value * 1000;
                        try
                        {
                            var length = p.Length;
                            if (length > 0 && seekMs >= length)
                                seekMs = Math.Max(0, length - 2000);
                            p.Time = seekMs;
                            _skipFrames = 3;
                            if (shouldPause)
                            {
                                await Task.Delay(100);
                                if (!_detached) p.Pause();
                            }
                        }
                        catch { _skipFrames = 0; }
                    });
                }
                else if (_pauseAfterSeek)
                {
                    _pauseAfterSeek = false;
                    var p = player;
                    Task.Run(async () =>
                    {
                        await Task.Delay(300);
                        if (p == null || _detached) return;
                        try { p.Pause(); } catch { }
                    });
                }
            };
            player.Paused += (_, _) =>
                Dispatcher.UIThread.Post(() => { if (!_detached) PlayPauseBtn.Content = "\u25b6"; });
            player.Stopped += (_, _) =>
                Dispatcher.UIThread.Post(() => { if (!_detached) PlayPauseBtn.Content = "\u25b6"; });
            player.EncounteredError += (_, _) =>
                ShowStatus("Video playback error");

            return player;
        }

        private PooledPlayer CreatePooledPlayer(LibVLC vlc, string path)
        {
            var pooled = new PooledPlayer
            {
                Buffer = new byte[FrameBytes]
            };
            pooled.Handle = GCHandle.Alloc(pooled.Buffer, GCHandleType.Pinned);

            var player = new MediaPlayer(vlc);
            player.SetVideoCallbacks(
                (opaque, planes) =>
                {
                    // Always write a valid pointer — VLC will crash if planes is left empty
                    if (_detached)
                    {
                        Marshal.WriteIntPtr(planes, DummyHandle.AddrOfPinnedObject());
                    }
                    else if (_activePooled == pooled)
                    {
                        Marshal.WriteIntPtr(planes, _bufHandle.AddrOfPinnedObject());
                    }
                    else
                    {
                        // Write to pooled player's own buffer if handle is still valid
                        var h = pooled.Handle;
                        if (h.IsAllocated)
                            Marshal.WriteIntPtr(planes, h.AddrOfPinnedObject());
                        else
                            Marshal.WriteIntPtr(planes, DummyHandle.AddrOfPinnedObject());
                    }
                    return IntPtr.Zero;
                },
                null,
                (opaque, picture) =>
                {
                    // Only render if this is the active pooled player
                    if (_activePooled == pooled)
                        Display(opaque, picture);
                });
            player.SetVideoFormat("RV32", W, H, W * 4);

            player.Playing += (_, _) =>
            {
                if (_activePooled == pooled)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (_detached) return;
                        HideStatus();
                        PlayPauseBtn.Content = "\u23f8";
                    });
                }

                if (_activePooled == pooled && _pendingSeek.HasValue)
                {
                    var shouldPause = _pauseAfterSeek;
                    _pauseAfterSeek = false;
                    Task.Run(async () =>
                    {
                        await Task.Delay(300);
                        if (_detached) return;
                        var seek = _pendingSeek;
                        if (!seek.HasValue) return;
                        _pendingSeek = null;
                        var seekMs = (long)seek.Value * 1000;
                        try
                        {
                            var length = player.Length;
                            if (length > 0 && seekMs >= length)
                                seekMs = Math.Max(0, length - 2000);
                            player.Time = seekMs;
                            _skipFrames = 3;
                            if (shouldPause)
                            {
                                await Task.Delay(100);
                                if (!_detached) player.Pause();
                            }
                        }
                        catch { _skipFrames = 0; }
                    });
                }
                else if (!pooled.Ready)
                {
                    // Initial preload: pause immediately
                    pooled.Ready = true;
                    Task.Run(async () =>
                    {
                        await Task.Delay(300);
                        if (_detached) return;
                        try { player.Pause(); } catch { }
                    });
                }
            };
            player.Paused += (_, _) =>
            {
                if (_activePooled == pooled)
                    Dispatcher.UIThread.Post(() => { if (!_detached) PlayPauseBtn.Content = "\u25b6"; });
            };

            pooled.Player = player;

            using var media = new Media(vlc, path, FromType.FromPath);
            player.Play(media);

            return pooled;
        }

        public void PausePlayback()
        {
            try { _player?.Pause(); } catch { }
        }

        public void PreloadVideos(IReadOnlyList<string> paths)
        {
            if (_detached || Program.LibVlc == null) return;

            _pool = new Dictionary<string, PooledPlayer>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path) || _pool.ContainsKey(path)) continue;
                if (!System.IO.File.Exists(path)) continue;

                try
                {
                    var pooled = CreatePooledPlayer(Program.LibVlc, path);
                    _pool[path] = pooled;
                }
                catch { }
            }
        }

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            _detached = true;
            var player = _player;
            _player = null;
            _bitmap = null;
            var pool = _pool;
            _pool = null;
            _activePooled = null;
            _activePooledPath = null;
            var handle = _bufHandle;

            // Collect all players to stop (avoid stopping the same player twice)
            var playersToDispose = new HashSet<MediaPlayer>();
            if (pool != null)
            {
                foreach (var kv in pool)
                    playersToDispose.Add(kv.Value.Player);
            }
            // Only add standalone player if it's not already in the pool
            if (player != null && !playersToDispose.Contains(player))
                playersToDispose.Add(player);

            var poolEntries = pool;

            Task.Run(async () =>
            {
                // Stop all players first
                foreach (var p in playersToDispose)
                {
                    try { p.Stop(); } catch { }
                    await Task.Delay(50);
                }

                // Wait for VLC callbacks to drain — they write to DummyBuf now,
                // so our real buffers are safe, but we still need players fully stopped
                await Task.Delay(500);

                // Dispose players (releases VLC resources)
                foreach (var p in playersToDispose)
                {
                    try { p.Dispose(); } catch { }
                }

                // Another delay to ensure all native callbacks are truly done
                await Task.Delay(200);

                // Now safe to free pinned buffers
                if (poolEntries != null)
                {
                    foreach (var kv in poolEntries)
                    {
                        try { if (kv.Value.Handle.IsAllocated) kv.Value.Handle.Free(); } catch { }
                    }
                }

                try { if (handle.IsAllocated) handle.Free(); } catch { }
            });
        }

        // ── Property changes ─────────────────────────────────────────────────

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == VideoPathProperty)
            {
                var path = change.GetNewValue<string?>();
                if (_player != null)
                    Load(path);
                else
                    _pendingPath = path;
            }
            else if (change.Property == VideoSeekSecondsProperty)
            {
                var secs = change.GetNewValue<int?>();
                if (secs.HasValue)
                    SeekTo(secs.Value);
            }
        }

        // ── Playback ─────────────────────────────────────────────────────────

        private void Load(string? path)
        {
            if (Program.LibVlc == null) return;

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                _activePooled?.Player.Pause();
                _activePooled = null;
                _activePooledPath = null;
                _player?.Stop();
                Dispatcher.UIThread.Post(() =>
                {
                    VideoImage.IsVisible = false;
                    StatusText.IsVisible = true;
                    StatusText.Text = string.IsNullOrEmpty(path) ? "No video" : $"File not found:\n{path}";
                });
                return;
            }

            // Try pooled player
            if (_pool != null && _pool.TryGetValue(path, out var pooled))
            {
                if (_activePooledPath != null &&
                    _activePooledPath.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    // Same video, just seek (handled by SeekTo via property change)
                    return;
                }

                // Pause previous pooled player
                if (_activePooled != null && _activePooled != pooled)
                {
                    try { _activePooled.Player.Pause(); } catch { }
                }

                _skipFrames = int.MaxValue;
                _activePooled = pooled;
                _activePooledPath = path;
                _player = pooled.Player;

                Dispatcher.UIThread.Post(() =>
                {
                    if (_detached) return;
                    VideoImage.IsVisible = false;
                    StatusText.IsVisible = false;
                });

                // Resume the pooled player — seek will happen via SeekTo
                if (pooled.Player.State == VLCState.Paused)
                    pooled.Player.Play();

                return;
            }

            // Fallback: single-player mode
            if (_player == null) return;

            try
            {
                _pauseAfterSeek = PauseOnLoad;
                if (!PauseOnLoad)
                {
                    _skipFrames = int.MaxValue;
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (_detached) return;
                        VideoImage.IsVisible = false;
                        StatusText.IsVisible = false;
                    });
                }
                else
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (_detached) return;
                        VideoImage.IsVisible = true;
                        StatusText.IsVisible = false;
                    });
                }
                using var media = new Media(Program.LibVlc, path, FromType.FromPath);
                _player.Play(media);
            }
            catch (Exception ex)
            {
                ShowStatus($"Video error: {ex.Message}");
            }
        }

        private CancellationTokenSource? _seekCts;

        private void SeekTo(int seconds)
        {
            var player = _player;
            if (player == null) return;

            _pendingSeek = seconds;

            try
            {
                var state = player.State;
                if (state == VLCState.Playing || state == VLCState.Paused)
                {
                    _seekCts?.Cancel();
                    _seekCts = new CancellationTokenSource();
                    var token = _seekCts.Token;
                    var isPooled = _activePooled != null;

                    Task.Run(async () =>
                    {
                        await Task.Delay(100, token);
                        if (token.IsCancellationRequested || _detached) return;
                        var seek = _pendingSeek;
                        if (!seek.HasValue) return;
                        _pendingSeek = null;
                        var ms = (long)seek.Value * 1000;
                        try
                        {
                            var length = player.Length;
                            if (length > 0 && ms >= length)
                                ms = Math.Max(0, length - 2000);
                            player.Time = ms;
                            if (isPooled)
                                _skipFrames = 3;
                        }
                        catch { }
                    }, token);
                }
            }
            catch { }
        }

        // ── VLC Callbacks ────────────────────────────────────────────────────

        private IntPtr Lock(IntPtr opaque, IntPtr planes)
        {
            if (_detached)
                Marshal.WriteIntPtr(planes, DummyHandle.AddrOfPinnedObject());
            else
                Marshal.WriteIntPtr(planes, _bufHandle.AddrOfPinnedObject());
            return IntPtr.Zero;
        }

        private void Display(IntPtr opaque, IntPtr picture)
        {
            if (_detached) return;
            var skip = _skipFrames;
            if (skip > 0)
            {
                Interlocked.Decrement(ref _skipFrames);
                if (skip == 1)
                    Dispatcher.UIThread.Post(() => { if (!_detached) VideoImage.IsVisible = true; });
                return;
            }
            if (Interlocked.CompareExchange(ref _uiReady, 0, 1) != 1) return;
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    if (_bitmap == null || _detached) return;
                    using var fb = _bitmap.Lock();
                    Marshal.Copy(_buf, 0, fb.Address, FrameBytes);
                    VideoImage.InvalidateVisual();
                }
                finally { Volatile.Write(ref _uiReady, 1); }
            }, DispatcherPriority.Render);
        }

        // ── Playback controls ─────────────────────────────────────────────────

        private void OnBack2sClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_player == null) return;
            _player.Time = Math.Max(0, _player.Time - 2000);
        }

        private void OnFrameBackClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_player == null) return;
            var fps = _player.Fps > 0 ? _player.Fps : 25f;
            var frameDuration = (long)(1000.0 / fps);
            _player.Time = Math.Max(0, _player.Time - frameDuration);
        }

        private void OnPlayPauseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
            _player?.Pause();

        private void OnFrameFwdClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
            _player?.NextFrame();

        private void OnFwd2sClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_player == null) return;
            _player.Time += 2000;
        }

        // ── Drawing ───────────────────────────────────────────────────────────

        private void OnPaintToggled(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _isPaintMode = PaintBtn.IsChecked == true;
            DrawingCanvas.IsHitTestVisible = _isPaintMode;
            ClearBtn.IsVisible = _isPaintMode;
        }

        private void OnClearClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
            DrawingCanvas.Children.Clear();

        private void DrawingCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!_isPaintMode) return;
            _currentStroke = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromRgb(255, 60, 60)),
                StrokeThickness = 3,
                StrokeLineCap = PenLineCap.Round,
                StrokeJoin = PenLineJoin.Round
            };
            DrawingCanvas.Children.Add(_currentStroke);
            _currentStroke.Points.Add(e.GetPosition(DrawingCanvas));
            e.Pointer.Capture(DrawingCanvas);
        }

        private void DrawingCanvas_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isPaintMode || _currentStroke == null) return;
            _currentStroke.Points.Add(e.GetPosition(DrawingCanvas));
        }

        private void DrawingCanvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _currentStroke = null;
            e.Pointer.Capture(null);
        }

        // ── UI helpers ───────────────────────────────────────────────────────

        private void ShowStatus(string text) =>
            Dispatcher.UIThread.Post(() =>
            {
                if (_detached) return;
                StatusText.Text = text;
                StatusText.IsVisible = true;
                VideoImage.IsVisible = false;
            });

        private void HideStatus() =>
            Dispatcher.UIThread.Post(() => { if (!_detached) StatusText.IsVisible = false; });
    }
}
