using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using VolleyStats.Data;
using VolleyStats.Data.Repositories;
using VolleyStats.Domain;

namespace VolleyStats.ViewModels
{
    public partial class HomePageViewModel : ViewModelBase
    {
        private readonly MatchSummaryLoader _matchSummaryLoader;
        private readonly TeamsRepository _teamsRepository;
        private readonly Func<MatchListItemViewModel, Task>? _openMatch;
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

        public IAsyncRelayCommand LoadMatchesCommand { get; }
        public IRelayCommand ClearTeamFilterCommand { get; }
        public IAsyncRelayCommand<MatchListItemViewModel> OpenMatchCommand { get; }

        public HomePageViewModel(MatchSummaryLoader matchSummaryLoader, TeamsRepository teamsRepository, Func<MatchListItemViewModel, Task>? openMatch = null)
        {
            _matchSummaryLoader = matchSummaryLoader;
            _teamsRepository = teamsRepository;
            _openMatch = openMatch;

            LoadMatchesCommand = new AsyncRelayCommand(LoadMatchesAsync);
            ClearTeamFilterCommand = new RelayCommand(ClearTeamFilter);
            OpenMatchCommand = new AsyncRelayCommand<MatchListItemViewModel>(OpenMatchAsync);
        }

        private async Task OpenMatchAsync(MatchListItemViewModel? item)
        {
            if (item == null || _openMatch == null) return;
            await _openMatch(item);
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
