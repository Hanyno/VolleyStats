using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using VolleyStats.Data;
using VolleyStats.Data.Repositories;
using VolleyStats.Domain;

namespace VolleyStats.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly MatchSummaryLoader _matchSummaryLoader;
        private readonly TeamsRepository _teamsRepository;
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
        public string TeamFilterButtonText => HasTeamFilter ? $"Tým: {TeamFilter}" : "Vybrat tým";

        public IAsyncRelayCommand LoadMatchesCommand { get; }
        public IRelayCommand ClearTeamFilterCommand { get; }

        public MainWindowViewModel(MatchSummaryLoader matchSummaryLoader, TeamsRepository teamsRepository)
        {
            _matchSummaryLoader = matchSummaryLoader;
            _teamsRepository = teamsRepository;

            LoadMatchesCommand = new AsyncRelayCommand(LoadMatchesAsync);
            ClearTeamFilterCommand = new RelayCommand(ClearTeamFilter);
        }

        public async Task InitializeAsync()
        {
            _teamsCache = _teamsRepository.GetAllTeamsWithPlayers().ToList();
            LoadSeasons();
            await LoadMatchesAsync();
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
