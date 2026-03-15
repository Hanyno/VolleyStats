using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using VolleyStats.Data;
using VolleyStats.Data.Repositories;
using VolleyStats.Models;

namespace VolleyStats.ViewModels
{
    public partial class HomePageViewModel : ViewModelBase
    {
        private readonly MatchSummaryLoader _matchSummaryLoader;
        private readonly TeamsRepository _teamsRepository;
        private readonly Func<MatchListItemViewModel, Task>? _openMatch;
        private readonly Func<Task>? _openSettings;
        private List<Team> _teamsCache = new();

        public ObservableCollection<string> AvailableSeasons { get; } = new();
        public ObservableCollection<MatchListItemViewModel> Matches { get; } = new();

        private string? _selectedSeason;
        public string? SelectedSeason
        {
            get => _selectedSeason;
            set
            {
                if (SetProperty(ref _selectedSeason, value))
                {
                    _ = LoadMatchesAsync();
                }
            }
        }

        private string? _teamFilter;
        public string? TeamFilter
        {
            get => _teamFilter;
            private set
            {
                if (SetProperty(ref _teamFilter, value))
                {
                    OnPropertyChanged(nameof(TeamFilterButtonText));
                    OnPropertyChanged(nameof(HasTeamFilter));
                }
            }
        }

        private string? _secondTeamFilter;
        private string? _teamFilterCode;
        private string? _secondTeamFilterCode;

        public bool HasTeamFilter => !string.IsNullOrWhiteSpace(TeamFilter);
        public string TeamFilterButtonText
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_secondTeamFilterCode))
                    return $"{_teamFilterCode ?? TeamFilter} / {_secondTeamFilterCode ?? _secondTeamFilter}";
                if (HasTeamFilter)
                    return _teamFilterCode ?? TeamFilter!;
                return "Filter by Team";
            }
        }

        private bool _hasSelectedMatches;
        public bool HasSelectedMatches
        {
            get => _hasSelectedMatches;
            private set => SetProperty(ref _hasSelectedMatches, value);
        }

        /// <summary>
        /// Delegate set by the view to show a team choice dialog.
        /// Parameters: (display1, value1, display2, value2). Returns chosen value or null.
        /// </summary>
        public Func<string, string, string, string, Task<string?>>? TriggerTeamChoiceDialog { get; set; }

        /// <summary>
        /// Delegate set by MainWindowViewModel to open analysis in the current tab.
        /// </summary>
        public Func<IReadOnlyList<string>, string, Task>? OpenAnalysis { get; set; }

        public IAsyncRelayCommand LoadMatchesCommand { get; }
        public IRelayCommand ClearTeamFilterCommand { get; }
        public IAsyncRelayCommand<MatchListItemViewModel> OpenMatchCommand { get; }
        public IAsyncRelayCommand OpenSettingsCommand { get; }
        public IAsyncRelayCommand AnalyzeCommand { get; }

        public HomePageViewModel(MatchSummaryLoader matchSummaryLoader, TeamsRepository teamsRepository,
            Func<MatchListItemViewModel, Task>? openMatch = null,
            Func<Task>? openSettings = null)
        {
            _matchSummaryLoader = matchSummaryLoader;
            _teamsRepository = teamsRepository;
            _openMatch = openMatch;
            _openSettings = openSettings;

            LoadMatchesCommand = new AsyncRelayCommand(LoadMatchesAsync);
            ClearTeamFilterCommand = new RelayCommand(ClearTeamFilter);
            OpenMatchCommand = new AsyncRelayCommand<MatchListItemViewModel>(OpenMatchAsync);
            OpenSettingsCommand = new AsyncRelayCommand(OpenSettingsAsync);
            AnalyzeCommand = new AsyncRelayCommand(AnalyzeAsync, () => HasSelectedMatches);

            Matches.CollectionChanged += OnMatchesCollectionChanged;
        }

        private void OnMatchesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (MatchListItemViewModel item in e.OldItems)
                    item.PropertyChanged -= OnMatchItemPropertyChanged;

            if (e.NewItems != null)
                foreach (MatchListItemViewModel item in e.NewItems)
                    item.PropertyChanged += OnMatchItemPropertyChanged;

            RefreshHasSelectedMatches();
        }

        private bool _updatingSelection;

        private async void OnMatchItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MatchListItemViewModel.IsSelected) || _updatingSelection)
                return;

            _updatingSelection = true;
            try
            {
                await HandleSelectionChanged();
            }
            finally
            {
                _updatingSelection = false;
            }
        }

        private async Task HandleSelectionChanged()
        {
            var selected = Matches.Where(m => m.IsSelected).ToList();
            HasSelectedMatches = selected.Count > 0;
            ((AsyncRelayCommand)AnalyzeCommand).NotifyCanExecuteChanged();

            // Already filtered to one team — no need to re-filter
            if (HasTeamFilter && _secondTeamFilter == null)
                return;

            if (selected.Count == 1 && string.IsNullOrWhiteSpace(TeamFilter))
            {
                // First selection: filter to matches containing either team
                var match = selected[0];
                ApplyTwoTeamFilter(match.HomeTeam, match.AwayTeam,
                    match.HomeTeamCode, match.AwayTeamCode);
                return;
            }

            if (selected.Count < 2) return;

            // Find common team across all selected matches
            var first = selected[0];
            var candidates = new List<(string name, string? code)>();
            if (selected.All(m => m.ContainsTeam(first.HomeTeam)))
                candidates.Add((first.HomeTeam, first.HomeTeamCode));
            if (selected.All(m => m.ContainsTeam(first.AwayTeam))
                && !candidates.Any(c => c.name.Equals(first.AwayTeam, StringComparison.OrdinalIgnoreCase)))
                candidates.Add((first.AwayTeam, first.AwayTeamCode));

            string? filterTeam = null;
            string? filterCode = null;
            if (candidates.Count == 1)
            {
                filterTeam = candidates[0].name;
                filterCode = candidates[0].code;
            }
            else if (candidates.Count == 2 && TriggerTeamChoiceDialog != null)
            {
                filterTeam = await TriggerTeamChoiceDialog(
                    FormatTeamDisplay(candidates[0].name, candidates[0].code), candidates[0].name,
                    FormatTeamDisplay(candidates[1].name, candidates[1].code), candidates[1].name);
                if (filterTeam != null)
                    filterCode = filterTeam.Equals(candidates[0].name, StringComparison.OrdinalIgnoreCase)
                        ? candidates[0].code : candidates[1].code;
            }
            else if (candidates.Count == 0)
            {
                var last = selected.Last();
                if (TriggerTeamChoiceDialog != null)
                {
                    filterTeam = await TriggerTeamChoiceDialog(
                        last.HomeTeamDisplay, last.HomeTeam,
                        last.AwayTeamDisplay, last.AwayTeam);
                    if (filterTeam != null)
                        filterCode = filterTeam.Equals(last.HomeTeam, StringComparison.OrdinalIgnoreCase)
                            ? last.HomeTeamCode : last.AwayTeamCode;
                }
            }

            if (!string.IsNullOrWhiteSpace(filterTeam))
                ApplyTeamFilter(filterTeam, filterCode, preserveSelection: true);
        }

        private async Task AnalyzeAsync()
        {
            var selected = Matches.Where(m => m.IsSelected).ToList();
            if (selected.Count == 0 || OpenAnalysis == null) return;

            string? team;
            if (selected.Count == 1)
            {
                var match = selected[0];
                if (TriggerTeamChoiceDialog != null)
                    team = await TriggerTeamChoiceDialog(
                        match.HomeTeamDisplay, match.HomeTeam,
                        match.AwayTeamDisplay, match.AwayTeam);
                else
                    return;
                if (string.IsNullOrWhiteSpace(team)) return;
            }
            else
            {
                team = TeamFilter;
                if (string.IsNullOrWhiteSpace(team)) return;
            }

            var filePaths = selected.Select(m => m.FilePath).ToList();
            await OpenAnalysis(filePaths, team);
        }

        private void RefreshHasSelectedMatches()
        {
            HasSelectedMatches = Matches.Any(m => m.IsSelected);
            ((AsyncRelayCommand)AnalyzeCommand).NotifyCanExecuteChanged();
        }

        private async Task OpenMatchAsync(MatchListItemViewModel? item)
        {
            if (item == null || _openMatch == null) return;
            await _openMatch(item);
        }

        private async Task OpenSettingsAsync()
        {
            if (_openSettings != null)
                await _openSettings();
        }

        public async Task InitializeAsync()
        {
            _teamsCache = _teamsRepository.GetAllTeamsWithPlayers().ToList();
            var hadSeason = !string.IsNullOrWhiteSpace(SelectedSeason);
            LoadSeasons();
            if (hadSeason)
            {
                await LoadMatchesAsync();
            }
        }

        private void LoadSeasons()
        {
            AvailableSeasons.Clear();

            foreach (var season in _matchSummaryLoader.GetAvailableSeasons())
            {
                AvailableSeasons.Add(season);
            }

            if (string.IsNullOrWhiteSpace(SelectedSeason))
            {
                SelectedSeason = AvailableSeasons.FirstOrDefault();
            }
        }

        public async Task LoadMatchesAsync()
        {
            // Remember which matches were selected before reload
            var selectedPaths = new HashSet<string>(
                Matches.Where(m => m.IsSelected).Select(m => m.FilePath),
                StringComparer.OrdinalIgnoreCase);

            Matches.Clear();

            // When two-team filter is active, load without filter and apply locally
            var loaderFilter = _secondTeamFilter != null ? null : TeamFilter;
            var summaries = await _matchSummaryLoader.LoadMatchesAsync(SelectedSeason, loaderFilter, _teamsCache);

            await ImportMissingTeamsAsync(summaries);

            foreach (var summary in summaries)
            {
                if (_secondTeamFilter != null)
                {
                    var matchesFirst = summary.HomeTeam.Contains(TeamFilter!, StringComparison.OrdinalIgnoreCase)
                                    || summary.AwayTeam.Contains(TeamFilter!, StringComparison.OrdinalIgnoreCase);
                    var matchesSecond = summary.HomeTeam.Contains(_secondTeamFilter, StringComparison.OrdinalIgnoreCase)
                                     || summary.AwayTeam.Contains(_secondTeamFilter, StringComparison.OrdinalIgnoreCase);
                    if (!matchesFirst && !matchesSecond) continue;
                }

                var item = new MatchListItemViewModel(summary);
                if (selectedPaths.Contains(summary.FilePath))
                    item.IsSelected = true;
                Matches.Add(item);
            }
        }

        private async Task ImportMissingTeamsAsync(IReadOnlyList<MatchSummary> summaries)
        {
            var knownCodes = new HashSet<string>(_teamsCache.Select(t => t.TeamCode), StringComparer.OrdinalIgnoreCase);

            // Collect team codes that are missing from the database
            var missingFiles = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in summaries)
            {
                if (!string.IsNullOrWhiteSpace(s.HomeTeamCode) && !knownCodes.Contains(s.HomeTeamCode)
                    && !string.IsNullOrWhiteSpace(s.FilePath))
                {
                    if (!missingFiles.TryGetValue(s.FilePath, out var codes))
                        missingFiles[s.FilePath] = codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    codes.Add(s.HomeTeamCode);
                }
                if (!string.IsNullOrWhiteSpace(s.AwayTeamCode) && !knownCodes.Contains(s.AwayTeamCode)
                    && !string.IsNullOrWhiteSpace(s.FilePath))
                {
                    if (!missingFiles.TryGetValue(s.FilePath, out var codes))
                        missingFiles[s.FilePath] = codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    codes.Add(s.AwayTeamCode);
                }
            }

            if (missingFiles.Count == 0) return;

            var parser = new DvwFileParser();
            var imported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (filePath, neededCodes) in missingFiles)
            {
                // Skip codes already imported from a previous file
                neededCodes.ExceptWith(imported);
                if (neededCodes.Count == 0) continue;

                Match match;
                try { match = await Task.Run(() => parser.ParseDvwFile(filePath)); }
                catch { continue; }

                foreach (var (matchTeam, players) in new[]
                {
                    (match.HomeTeam, match.HomePlayers),
                    (match.AwayTeam, match.AwayPlayers)
                })
                {
                    if (matchTeam == null || string.IsNullOrWhiteSpace(matchTeam.TeamCode)) continue;
                    if (!neededCodes.Contains(matchTeam.TeamCode)) continue;
                    if (imported.Contains(matchTeam.TeamCode)) continue;

                    var team = new Team
                    {
                        TeamCode = matchTeam.TeamCode,
                        Name = matchTeam.Name,
                        CoachName = matchTeam.CoachName,
                        AssistantCoachName = matchTeam.AssistantCoachName,
                        Players = players?.Select(mp => new Player
                        {
                            JerseyNumber = mp.JerseyNumber,
                            ExternalPlayerId = mp.ExternalPlayerId,
                            LastName = mp.LastName,
                            FirstName = mp.FirstName,
                            BirthDate = mp.BirthDate.HasValue
                                ? new DateTimeOffset(mp.BirthDate.Value.ToDateTime(TimeOnly.MinValue))
                                : null,
                            HeightCm = mp.HeightCm,
                            Position = mp.Position,
                            PlayerRole = mp.PlayerRole,
                            NickName = mp.NickName,
                            IsForeign = mp.IsForeign,
                            TransferredOut = mp.TransferredOut,
                            BirthDateSerial = mp.BirthDateSerial
                        }).ToList() ?? new List<Player>()
                    };

                    _teamsRepository.SaveTeam(team);
                    imported.Add(matchTeam.TeamCode);
                }
            }

            if (imported.Count > 0)
                _teamsCache = _teamsRepository.GetAllTeamsWithPlayers().ToList();
        }

        public void ApplyTeamFilter(string? teamName, string? teamCode = null, bool preserveSelection = false)
        {
            _updatingSelection = true;
            try
            {
                if (!preserveSelection)
                {
                    var isSameTeam = !string.IsNullOrWhiteSpace(teamName)
                                  && teamName.Equals(TeamFilter, StringComparison.OrdinalIgnoreCase)
                                  && _secondTeamFilter == null;

                    if (!isSameTeam)
                    {
                        foreach (var m in Matches)
                            m.IsSelected = false;
                    }
                }

                _secondTeamFilter = null;
                _secondTeamFilterCode = null;
                _teamFilterCode = teamCode;
                TeamFilter = string.IsNullOrWhiteSpace(teamName) ? null : teamName;
                OnPropertyChanged(nameof(TeamFilterButtonText));
            }
            finally
            {
                _updatingSelection = false;
            }
            _ = LoadMatchesAsync();
        }

        private void ApplyTwoTeamFilter(string team1, string team2, string? code1, string? code2)
        {
            TeamFilter = team1;
            _secondTeamFilter = team2;
            _teamFilterCode = code1;
            _secondTeamFilterCode = code2;
            OnPropertyChanged(nameof(TeamFilterButtonText));
            _ = LoadMatchesAsync();
        }

        private static string FormatTeamDisplay(string name, string? code) =>
            string.IsNullOrEmpty(code) ? name : $"{name} ({code})";

        private void ClearTeamFilter()
        {
            if (TeamFilter != null || _secondTeamFilter != null)
            {
                _updatingSelection = true;
                try
                {
                    foreach (var m in Matches)
                        m.IsSelected = false;
                    _secondTeamFilter = null;
                    _secondTeamFilterCode = null;
                    _teamFilterCode = null;
                    TeamFilter = null;
                    OnPropertyChanged(nameof(TeamFilterButtonText));
                }
                finally
                {
                    _updatingSelection = false;
                }
                _ = LoadMatchesAsync();
            }
        }
    }
}
