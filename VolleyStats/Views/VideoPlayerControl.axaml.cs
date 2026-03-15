using System;
using System.Runtime.InteropServices;
using System.Threading;
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

        private const uint W = 1280, H = 720;
        private const int FrameBytes = (int)(W * H * 4);
        private readonly byte[] _buf = new byte[FrameBytes];
        private GCHandle _bufHandle;
        private WriteableBitmap? _bitmap;
        private int _uiReady = 1;

        private string? _pendingPath;
        private int? _pendingSeek;

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
            _bufHandle = GCHandle.Alloc(_buf, GCHandleType.Pinned);

            await Program.LibVlcInitTask;

            var vlc = Program.LibVlc;
            if (vlc == null)
            {
                Console.WriteLine("[VIDEO] LibVLC init failed — cannot play video");
                ShowStatus("Video engine unavailable");
                return;
            }

            _player = new MediaPlayer(vlc);
            _bitmap = new WriteableBitmap(
                new PixelSize((int)W, (int)H), new Vector(96, 96),
                PixelFormats.Bgra8888, AlphaFormat.Opaque);

            VideoImage.Source = _bitmap;
            _player.SetVideoCallbacks(Lock, null, Display);
            _player.SetVideoFormat("RV32", W, H, W * 4);

            _player.Playing += (_, _) =>
            {
                if (_pendingSeek.HasValue)
                {
                    _player.Time = (long)_pendingSeek.Value * 1000;
                    _pendingSeek = null;
                }
                Dispatcher.UIThread.Post(() =>
                {
                    HideStatus();
                    PlayPauseBtn.Content = "⏸";
                });
            };
            _player.Paused += (_, _) => Dispatcher.UIThread.Post(() => PlayPauseBtn.Content = "▶");
            _player.Stopped += (_, _) => Dispatcher.UIThread.Post(() => PlayPauseBtn.Content = "▶");

            var path = _pendingPath ?? VideoPath;
            Console.WriteLine($"[VIDEO] OnAttached complete — loading path: '{path}'");
            if (path != null)
                Load(path);
        }

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            _player?.Stop();
            _player?.Dispose();
            _player = null;
            if (_bufHandle.IsAllocated) _bufHandle.Free();
        }

        // ── Property changes ─────────────────────────────────────────────────

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == VideoPathProperty)
            {
                var path = change.GetNewValue<string?>();
                Console.WriteLine($"[VIDEO] VideoPath changed -> '{path}', player ready: {_player != null}");
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
            if (_player == null || Program.LibVlc == null) return;

            Console.WriteLine($"[VIDEO] Load('{path}') file exists: {(path != null ? System.IO.File.Exists(path).ToString() : "n/a")}");

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                _player.Stop();
                Dispatcher.UIThread.Post(() =>
                {
                    VideoImage.IsVisible = false;
                    StatusText.IsVisible = true;
                    StatusText.Text = string.IsNullOrEmpty(path) ? "No video" : $"File not found:\n{path}";
                });
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                VideoImage.IsVisible = true;
                StatusText.IsVisible = false;
            });

            using var media = new Media(Program.LibVlc, path, FromType.FromPath);
            _player.Play(media);
        }

        private void SeekTo(int seconds)
        {
            if (_player == null) return;
            var ms = (long)seconds * 1000;
            var state = _player.State;
            if (state == VLCState.Playing || state == VLCState.Paused)
                _player.Time = ms;
            else
                _pendingSeek = seconds;
        }

        // ── VLC Callbacks ────────────────────────────────────────────────────

        private IntPtr Lock(IntPtr opaque, IntPtr planes)
        {
            Marshal.WriteIntPtr(planes, _bufHandle.AddrOfPinnedObject());
            return IntPtr.Zero;
        }

        private void Display(IntPtr opaque, IntPtr picture)
        {
            if (Interlocked.CompareExchange(ref _uiReady, 0, 1) != 1) return;
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    if (_bitmap == null) return;
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
                StatusText.Text = text;
                StatusText.IsVisible = true;
                VideoImage.IsVisible = false;
            });

        private void HideStatus() =>
            Dispatcher.UIThread.Post(() => StatusText.IsVisible = false);
    }
}
