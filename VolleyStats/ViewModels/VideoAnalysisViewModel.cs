using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public IRelayCommand BackCommand { get; }
        public IRelayCommand IncrementBeforeCommand { get; }
        public IRelayCommand DecrementBeforeCommand { get; }
        public IRelayCommand IncrementAfterCommand { get; }
        public IRelayCommand DecrementAfterCommand { get; }

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
            var delayMs = (_secondsBefore + _secondsAfter) * 1000;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delayMs, cts.Token);
                    if (cts.Token.IsCancellationRequested) return;
                    AdvanceToNextCode();
                }
                catch (TaskCanceledException) { }
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
