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

        public bool HasTeamFilter => !string.IsNullOrWhiteSpace(TeamFilter);
        public string TeamFilterButtonText => HasTeamFilter ? $"Team: {TeamFilter}" : "Filter by Team";

        private bool _hasSelectedMatches;
        public bool HasSelectedMatches
        {
            get => _hasSelectedMatches;
            private set => SetProperty(ref _hasSelectedMatches, value);
        }

        public IAsyncRelayCommand LoadMatchesCommand { get; }
        public IRelayCommand ClearTeamFilterCommand { get; }
        public IAsyncRelayCommand<MatchListItemViewModel> OpenMatchCommand { get; }
        public IAsyncRelayCommand OpenSettingsCommand { get; }

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

        private void OnMatchItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MatchListItemViewModel.IsSelected))
                RefreshHasSelectedMatches();
        }

        private void RefreshHasSelectedMatches() =>
            HasSelectedMatches = Matches.Any(m => m.IsSelected);

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
            Matches.Clear();

            var summaries = await _matchSummaryLoader.LoadMatchesAsync(SelectedSeason, TeamFilter, _teamsCache);
            foreach (var summary in summaries)
            {
                Matches.Add(new MatchListItemViewModel(summary));
            }
        }

        public void ApplyTeamFilter(string? teamName)
        {
            TeamFilter = string.IsNullOrWhiteSpace(teamName) ? null : teamName;
            _ = LoadMatchesAsync();
        }

        private void ClearTeamFilter()
        {
            if (TeamFilter != null)
            {
                TeamFilter = null;
                _ = LoadMatchesAsync();
            }
        }
    }
}
