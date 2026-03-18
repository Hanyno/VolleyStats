using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using VolleyStats.Data;
using VolleyStats.Enums;
using VolleyStats.Models;

namespace VolleyStats.ViewModels
{
    public class VideoAnalysisCodeItem
    {
        public BallContactCode Code { get; }
        public string DisplayCode { get; }
        public string MatchLabel { get; }
        public string? VideoPath { get; }
        public int? VideoSecond { get; }

        public VideoAnalysisCodeItem(BallContactCode code, string displayCode, string matchLabel, string? videoPath, int? videoSecond)
        {
            Code = code;
            DisplayCode = displayCode;
            MatchLabel = matchLabel;
            VideoPath = videoPath;
            VideoSecond = videoSecond;
        }
    }

    public class VideoAnalysisViewModel : ViewModelBase
    {
        private readonly Func<Task> _navigateBack;
        private readonly List<VideoAnalysisCodeItem> _allCodes = new();
        private CancellationTokenSource? _autoAdvanceCts;
        private CancellationTokenSource? _renderCts;
        private readonly AppSettingsStore _settingsStore = new();

        public event EventHandler? InitializationCompleted;
        public event EventHandler? PauseVideoRequested;

        public string AnalysisTeam { get; }

        private string _teamFilter = string.Empty;
        public string TeamFilter
        {
            get => _teamFilter;
            set { if (SetProperty(ref _teamFilter, value)) ApplyFilter(); }
        }

        private string _playerFilter = string.Empty;
        public string PlayerFilter
        {
            get => _playerFilter;
            set { if (SetProperty(ref _playerFilter, value)) ApplyFilter(); }
        }

        private string _skillFilter = string.Empty;
        public string SkillFilter
        {
            get => _skillFilter;
            set { if (SetProperty(ref _skillFilter, value)) ApplyFilter(); }
        }

        private string _hitFilter = string.Empty;
        public string HitFilter
        {
            get => _hitFilter;
            set { if (SetProperty(ref _hitFilter, value)) ApplyFilter(); }
        }

        private string _evalFilter = string.Empty;
        public string EvalFilter
        {
            get => _evalFilter;
            set { if (SetProperty(ref _evalFilter, value)) ApplyFilter(); }
        }

        private int _secondsBefore = 2;
        public int SecondsBefore
        {
            get => _secondsBefore;
            set => SetProperty(ref _secondsBefore, Math.Max(0, value));
        }

        private int _secondsAfter = 4;
        public int SecondsAfter
        {
            get => _secondsAfter;
            set => SetProperty(ref _secondsAfter, Math.Max(0, value));
        }

        private string? _videoSourcePath;
        public string? VideoSourcePath
        {
            get => _videoSourcePath;
            private set => SetProperty(ref _videoSourcePath, value);
        }

        private int? _videoSeekSeconds;
        public int? VideoSeekSeconds
        {
            get => _videoSeekSeconds;
            private set => SetProperty(ref _videoSeekSeconds, value);
        }

        public ObservableCollection<VideoAnalysisCodeItem> FilteredCodes { get; } = new();

        private VideoAnalysisCodeItem? _selectedCode;
        public VideoAnalysisCodeItem? SelectedCode
        {
            get => _selectedCode;
            set
            {
                if (SetProperty(ref _selectedCode, value) && value != null)
                    SeekToCode(value);
            }
        }

        // Video playback state — set by the View when player fires Playing/Paused
        private volatile bool _isVideoPlaying;
        public bool IsVideoPlaying
        {
            get => _isVideoPlaying;
            set => _isVideoPlaying = value;
        }

        // Render state
        private bool _isRendering;
        public bool IsRendering
        {
            get => _isRendering;
            set => SetProperty(ref _isRendering, value);
        }

        private double _renderProgress;
        public double RenderProgress
        {
            get => _renderProgress;
            set => SetProperty(ref _renderProgress, value);
        }

        private string _renderStatusText = "";
        public string RenderStatusText
        {
            get => _renderStatusText;
            set
            {
                if (SetProperty(ref _renderStatusText, value))
                    OnPropertyChanged(nameof(HasRenderStatus));
            }
        }

        public bool HasRenderStatus => !string.IsNullOrEmpty(_renderStatusText);

        // Queue
        public RenderQueueService RenderQueue => RenderQueueService.Instance;

        // Event for the View to handle the save dialog
        public Func<FilePickerSaveOptions, Task<string?>>? RequestSavePath { get; set; }

        // Event for the View to show a message dialog
        public Func<string, string, Task>? RequestShowMessage { get; set; }

        public IRelayCommand BackCommand { get; }
        public IRelayCommand IncrementBeforeCommand { get; }
        public IRelayCommand DecrementBeforeCommand { get; }
        public IRelayCommand IncrementAfterCommand { get; }
        public IRelayCommand DecrementAfterCommand { get; }
        public IAsyncRelayCommand RenderVideoCommand { get; }
        public IRelayCommand CancelRenderCommand { get; }
        public IAsyncRelayCommand AddToQueueCommand { get; }
        public IAsyncRelayCommand StartQueueCommand { get; }
        public IRelayCommand CancelQueueCommand { get; }

        private readonly IReadOnlyList<string> _matchFilePaths;

        public VideoAnalysisViewModel(IReadOnlyList<string> matchFilePaths, string analysisTeam, Func<Task> navigateBack)
        {
            _matchFilePaths = matchFilePaths;
            _navigateBack = navigateBack;
            AnalysisTeam = analysisTeam;

            BackCommand = new AsyncRelayCommand(() => _navigateBack());
            IncrementBeforeCommand = new RelayCommand(() => SecondsBefore++);
            DecrementBeforeCommand = new RelayCommand(() => SecondsBefore--);
            IncrementAfterCommand = new RelayCommand(() => SecondsAfter++);
            DecrementAfterCommand = new RelayCommand(() => SecondsAfter--);
            RenderVideoCommand = new AsyncRelayCommand(RenderVideoAsync, CanRenderVideo);
            CancelRenderCommand = new RelayCommand(CancelRender);
            AddToQueueCommand = new AsyncRelayCommand(AddToQueueAsync, () => FilteredCodes.Count > 0);
            StartQueueCommand = new AsyncRelayCommand(StartQueueAsync, () => RenderQueue.HasJobs && !RenderQueue.IsProcessing);
            CancelQueueCommand = new RelayCommand(() => RenderQueue.CancelProcessing());

            RenderQueue.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(RenderQueueService.HasJobs) or nameof(RenderQueueService.IsProcessing))
                    StartQueueCommand.NotifyCanExecuteChanged();
            };
        }

        private bool CanRenderVideo() => FilteredCodes.Count > 0 && !IsRendering;

        private void CancelRender()
        {
            _renderCts?.Cancel();
        }

        private List<VideoSegment> BuildSegments()
        {
            return FilteredCodes
                .Where(c => c.VideoPath != null && c.VideoSecond.HasValue)
                .Select(c => new VideoSegment
                {
                    FilePath = c.VideoPath!,
                    StartSeconds = Math.Max(0, c.VideoSecond!.Value - SecondsBefore),
                    EndSeconds = c.VideoSecond!.Value + SecondsAfter
                })
                .ToList();
        }

        private string BuildFilterDescription()
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(_teamFilter)) parts.Add($"T={_teamFilter}");
            if (!string.IsNullOrEmpty(_playerFilter)) parts.Add($"P={_playerFilter}");
            if (!string.IsNullOrEmpty(_skillFilter)) parts.Add($"S={_skillFilter}");
            if (!string.IsNullOrEmpty(_hitFilter)) parts.Add($"H={_hitFilter}");
            if (!string.IsNullOrEmpty(_evalFilter)) parts.Add($"E={_evalFilter}");
            var filter = parts.Count > 0 ? string.Join(" ", parts) : "all";
            return $"{AnalysisTeam} [{filter}] ({FilteredCodes.Count} codes)";
        }

        private async Task AddToQueueAsync()
        {
            var segments = BuildSegments();
            if (segments.Count == 0) return;

            if (RequestSavePath == null) return;

            var saveOptions = new FilePickerSaveOptions
            {
                Title = "Save Rendered Video",
                SuggestedFileName = $"VideoAnalysis_{AnalysisTeam}.mp4",
                DefaultExtension = "mp4",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("MP4 Video") { Patterns = new[] { "*.mp4" } }
                }
            };

            var outputPath = await RequestSavePath(saveOptions);
            if (string.IsNullOrEmpty(outputPath)) return;

            var job = new RenderJob
            {
                Name = BuildFilterDescription(),
                OutputPath = outputPath,
                Segments = segments
            };

            RenderQueue.AddJob(job);
            RenderStatusText = $"Added to queue ({RenderQueue.Jobs.Count} jobs)";
        }

        private async Task StartQueueAsync()
        {
            var encoderMode = _settingsStore.LoadVideoEncoderMode();
            var ffmpegPath = _settingsStore.LoadFfmpegPath();

            Console.Error.WriteLine($"[RenderVM] Starting queue: {RenderQueue.Jobs.Count} jobs, encoder={encoderMode}");

            await RenderQueue.ProcessQueueAsync(encoderMode, ffmpegPath, () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    RenderStatusText = RenderQueue.QueueStatusText;
                    RequestShowMessage?.Invoke("Render Queue Complete", RenderQueue.QueueStatusText);
                });
            });
        }

        private async Task RenderVideoAsync()
        {
            Console.Error.WriteLine($"[RenderVM] RenderVideoAsync called, FilteredCodes.Count={FilteredCodes.Count}");

            var segments = BuildSegments();

            Console.Error.WriteLine($"[RenderVM] Built {segments.Count} segments (SecondsBefore={SecondsBefore}, SecondsAfter={SecondsAfter})");
            foreach (var seg in segments)
                Console.Error.WriteLine($"[RenderVM]   Segment: {seg.FilePath} [{seg.StartSeconds}s - {seg.EndSeconds}s]");

            if (segments.Count == 0)
            {
                Console.Error.WriteLine($"[RenderVM] No segments, aborting");
                return;
            }

            if (RequestSavePath == null)
            {
                Console.Error.WriteLine($"[RenderVM] RequestSavePath is null, aborting");
                return;
            }

            var saveOptions = new FilePickerSaveOptions
            {
                Title = "Save Rendered Video",
                SuggestedFileName = $"VideoAnalysis_{AnalysisTeam}.mp4",
                DefaultExtension = "mp4",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("MP4 Video") { Patterns = new[] { "*.mp4" } }
                }
            };

            var outputPath = await RequestSavePath(saveOptions);
            Console.Error.WriteLine($"[RenderVM] Save path: {outputPath ?? "(cancelled)"}");
            if (string.IsNullOrEmpty(outputPath)) return;

            var encoderMode = _settingsStore.LoadVideoEncoderMode();
            var ffmpegPath = _settingsStore.LoadFfmpegPath();
            Console.Error.WriteLine($"[RenderVM] EncoderMode={encoderMode}, FfmpegPath={ffmpegPath ?? "(null)"}");

            IsRendering = true;
            RenderProgress = 0;
            RenderStatusText = "Starting...";
            RenderVideoCommand.NotifyCanExecuteChanged();
            _renderCts = new CancellationTokenSource();

            var progress = new Progress<VideoRenderProgress>(p =>
            {
                RenderProgress = p.OverallPercent;
                RenderStatusText = $"{p.Phase}: {p.CurrentSegment}/{p.TotalSegments}";
                Console.Error.WriteLine($"[RenderVM] Progress: {p.OverallPercent:F1}% - {p.Phase}: {p.CurrentSegment}/{p.TotalSegments}");
            });

            try
            {
                var renderer = new VideoRendererService();
                await renderer.RenderAsync(segments, outputPath, encoderMode, ffmpegPath, progress, _renderCts.Token);
                RenderStatusText = "Render complete!";
                Console.Error.WriteLine($"[RenderVM] Render complete!");
            }
            catch (OperationCanceledException)
            {
                RenderStatusText = "Render cancelled.";
                Console.Error.WriteLine($"[RenderVM] Render cancelled by user");
                try { if (File.Exists(outputPath)) File.Delete(outputPath); } catch { }
            }
            catch (Exception ex)
            {
                RenderStatusText = $"Error: {ex.Message}";
                Console.Error.WriteLine($"[RenderVM] Render FAILED: {ex.GetType().Name}: {ex.Message}");
                Console.Error.WriteLine($"[RenderVM] Stack: {ex.StackTrace}");
                if (ex.InnerException != null)
                    Console.Error.WriteLine($"[RenderVM] Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            finally
            {
                IsRendering = false;
                _renderCts = null;
                RenderVideoCommand.NotifyCanExecuteChanged();
            }
        }

        public async Task InitializeAsync()
        {
            var parser = new DvwFileParser();

            foreach (var filePath in _matchFilePaths)
            {
                Match match;
                try { match = await Task.Run(() => parser.ParseDvwFile(filePath)); }
                catch { continue; }

                bool isHomeTeam = match.HomeTeam?.Name?.Equals(AnalysisTeam, StringComparison.OrdinalIgnoreCase) == true;
                bool isAwayTeam = match.AwayTeam?.Name?.Equals(AnalysisTeam, StringComparison.OrdinalIgnoreCase) == true;
                if (!isHomeTeam && !isAwayTeam) continue;

                var matchLabel = $"{match.HomeTeam?.Name ?? "?"} vs {match.AwayTeam?.Name ?? "?"}";

                foreach (var code in match.ScoutCodes)
                {
                    if (code is not BallContactCode bc) continue;

                    var codeTeam = bc.Team;
                    bool isAnalysisTeamCode;
                    if (isHomeTeam)
                        isAnalysisTeamCode = codeTeam == TeamSide.Home;
                    else
                        isAnalysisTeamCode = codeTeam == TeamSide.Away;

                    var displayTeam = isAnalysisTeamCode ? "*" : "a";
                    var playerNum = bc.PlayerNumber?.ToString("D2") ?? "??";
                    var skill = SkillToChar(bc.Skill);
                    var hit = bc.HitType?.ToString() ?? "";
                    var eval = EvalToChar(bc.Evaluation);
                    var displayCode = $"{displayTeam}{playerNum}{skill}{hit}{eval}";

                    string? videoPath = null;
                    if (bc.VideoFile.HasValue && match.VideoPaths != null)
                    {
                        var idx = bc.VideoFile.Value - 1;
                        if (idx >= 0 && idx < match.VideoPaths.Count)
                            videoPath = ResolveVideoPath(match.VideoPaths[idx], filePath);
                    }

                    _allCodes.Add(new VideoAnalysisCodeItem(bc, displayCode, matchLabel, videoPath, bc.VideoSecond));
                }
            }

            AllVideoPaths = _allCodes
                .Select(c => c.VideoPath)
                .Where(p => p != null)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()!;

            ApplyFilter();
            InitializationCompleted?.Invoke(this, EventArgs.Empty);
        }

        public IReadOnlyList<string> AllVideoPaths { get; private set; } = Array.Empty<string>();

        private bool HasAnyFilter =>
            !string.IsNullOrEmpty(_teamFilter) ||
            !string.IsNullOrEmpty(_playerFilter) ||
            !string.IsNullOrEmpty(_skillFilter) ||
            !string.IsNullOrEmpty(_hitFilter) ||
            !string.IsNullOrEmpty(_evalFilter);

        private void ApplyFilter()
        {
            FilteredCodes.Clear();
            if (!HasAnyFilter) return;

            foreach (var item in _allCodes)
            {
                if (!MatchesFilter(item)) continue;
                FilteredCodes.Add(item);
            }

            RenderVideoCommand.NotifyCanExecuteChanged();
            AddToQueueCommand.NotifyCanExecuteChanged();
        }

        private bool MatchesFilter(VideoAnalysisCodeItem item)
        {
            var bc = item.Code;

            if (!string.IsNullOrEmpty(_teamFilter))
            {
                var display = item.DisplayCode;
                if (display.Length > 0 && !display[0].ToString().Equals(_teamFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!string.IsNullOrEmpty(_playerFilter))
            {
                var playerStr = bc.PlayerNumber?.ToString("D2") ?? "";
                if (!playerStr.Contains(_playerFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!string.IsNullOrEmpty(_skillFilter))
            {
                var skillChar = SkillToChar(bc.Skill);
                if (!skillChar.Equals(_skillFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!string.IsNullOrEmpty(_hitFilter))
            {
                var hitStr = bc.HitType?.ToString() ?? "";
                if (!hitStr.Equals(_hitFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!string.IsNullOrEmpty(_evalFilter))
            {
                var evalChar = EvalToChar(bc.Evaluation);
                if (!evalChar.Equals(_evalFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private void SeekToCode(VideoAnalysisCodeItem item)
        {
            _autoAdvanceCts?.Cancel();

            if (item.VideoPath != null)
                VideoSourcePath = item.VideoPath;

            if (item.VideoSecond.HasValue)
            {
                var seekVal = Math.Max(0, item.VideoSecond.Value - _secondsBefore);
                VideoSeekSeconds = null;
                VideoSeekSeconds = seekVal;
            }

            StartAutoAdvanceTimer();
        }

        private void StartAutoAdvanceTimer()
        {
            var cts = new CancellationTokenSource();
            _autoAdvanceCts = cts;
            var totalMs = (_secondsBefore + _secondsAfter) * 1000;

            Task.Run(async () =>
            {
                try
                {
                    var elapsed = 0;
                    const int tickMs = 100;
                    while (elapsed < totalMs)
                    {
                        cts.Token.ThrowIfCancellationRequested();
                        await Task.Delay(tickMs, cts.Token);

                        // Only count down while the video is actually playing
                        if (_isVideoPlaying)
                            elapsed += tickMs;
                    }

                    AdvanceToNextCode();
                }
                catch (OperationCanceledException) { }
            }, cts.Token);
        }

        private void AdvanceToNextCode()
        {
            Dispatcher.UIThread.Post(() =>
            {
                var current = _selectedCode;
                if (current == null || FilteredCodes.Count == 0) return;

                var idx = FilteredCodes.IndexOf(current);
                if (idx < 0) return;

                if (idx >= FilteredCodes.Count - 1)
                {
                    PauseVideoRequested?.Invoke(this, EventArgs.Empty);
                    return;
                }

                SelectedCode = FilteredCodes[idx + 1];
            });
        }

        private static string? ResolveVideoPath(string rawPath, string matchFilePath)
        {
            if (string.IsNullOrWhiteSpace(rawPath)) return null;
            if (Path.IsPathRooted(rawPath) && File.Exists(rawPath)) return rawPath;
            var dir = Path.GetDirectoryName(matchFilePath);
            if (dir != null)
            {
                var combined = Path.Combine(dir, rawPath);
                if (File.Exists(combined)) return combined;
            }
            return rawPath;
        }

        private static string SkillToChar(Skill skill) => skill switch
        {
            Skill.Serve => "S",
            Skill.Reception => "R",
            Skill.Attack => "A",
            Skill.Block => "B",
            Skill.Dig => "D",
            Skill.Set => "E",
            Skill.FreeBall => "F",
            _ => ""
        };

        private static string EvalToChar(Evaluation? eval) => eval switch
        {
            Evaluation.Error => "=",
            Evaluation.VeryPoorOrBlocked => "/",
            Evaluation.Poor => "-",
            Evaluation.InsufficientOrCovered => "!",
            Evaluation.Positive => "+",
            Evaluation.Point => "#",
            _ => ""
        };
    }
}
