using Avalonia.Controls;
using System;
using System.Linq;
using VolleyStats.Domain;
using VolleyStats.DTO;
using VolleyStats.Enums;
using VolleyStats.Services;

namespace VolleyStats.Views
{
    public partial class TeamAnalysisView : UserControl
    {
        private readonly ITeamAnalysisService _analysisService;
        private readonly IOfficialStatsService _matchService;

        public event EventHandler? HomeRequested;

        private Team? _currentTeam;
        private DateTime? _currentFromUtc;
        private DateTime? _currentToUtc;
        private int? _currentLimitLastMatches;
        private int? _currentCompetitionId;

        private TeamBasicOverviewDto? _currentOverview;
        private SkillAnalysisDto? _currentSkillAnalysis;

        public TeamAnalysisView(ITeamAnalysisService analysisService, IOfficialStatsService matchService)
        {
            InitializeComponent();

            _matchService = matchService;
            _analysisService = analysisService;

            InitChildControls();
        }

        private void InitChildControls()
        {
            var teams = _matchService.GetAllTeams().ToList();
            FilterView.Initialize(teams);

            FilterView.GenerateRequested += FilterView_OnGenerateRequested;

            ResultsView.ExportRequested += ResultsView_OnExportRequested;
            ResultsView.HomeRequested += ResultsView_OnHomeRequested;
            ResultsView.FilterRequested += ResultsView_OnFilterRequested;

            ShowFilterView();
        }

        private void ShowFilterView()
        {
            FilterView.IsVisible = true;
            ResultsView.IsVisible = false;
        }

        private void ShowResultsView()
        {
            FilterView.IsVisible = false;
            ResultsView.IsVisible = true;
        }

        private void FilterView_OnGenerateRequested(object? sender, TeamAnalysisFilterEventArgs e)
        {
            _currentTeam = e.Team;
            if (_currentTeam == null)
            {
                ResultsView.ClearAll();
                ShowFilterView();
                return;
            }

            ResolvePeriod(e.PeriodText, out _currentFromUtc, out _currentToUtc, out _currentLimitLastMatches);
            _currentCompetitionId = e.CompetitionId;

            var overview = _analysisService.GetBasicOverview(
                _currentTeam.Id,
                _currentFromUtc,
                _currentToUtc,
                _currentCompetitionId,
                includeHome: true,
                includeAway: true,
                limitLastMatches: _currentLimitLastMatches);

            _currentOverview = overview;

            if (overview.MatchesPlayed == 0)
            {
                ResultsView.ShowNoData();
                ResultsView.ClearAll();
                ShowFilterView();
                return;
            }

            var playerStats = _analysisService.GetPlayersStats(
                _currentTeam.Id,
                _currentFromUtc,
                _currentToUtc,
                _currentCompetitionId,
                includeHome: true,
                includeAway: true,
                limitLastMatches: _currentLimitLastMatches);

            ResultsView.ShowOverview(_currentTeam, overview);
            ResultsView.ShowPlayersStatistics(playerStats);
            ShowResultsView();
        }

        private void ResolvePeriod(string? periodText,
            out DateTime? fromUtc, out DateTime? toUtc, out int? limitLastMatches)
        {
            fromUtc = null;
            toUtc = null;
            limitLastMatches = null;

            switch (periodText)
            {
                case "Current season":
                    var now = DateTime.UtcNow;
                    int yearStart = now.Month >= 9 ? now.Year : now.Year - 1;
                    fromUtc = new DateTime(yearStart, 9, 1, 0, 0, 0, DateTimeKind.Utc);
                    break;

                case "Last 5 matches":
                    limitLastMatches = 5;
                    break;

            }
        }

        private void ResultsView_OnExportRequested(object? sender, EventArgs e)
        {
            if (_currentTeam == null || _currentOverview == null)
                return;

            // Here you will call your IAnalysisPdfExporter
            // and pass _currentTeam, _currentOverview, _currentSkillAnalysis
        }

        private void ResultsView_OnHomeRequested(object? sender, EventArgs e)
        {
            HomeRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ResultsView_OnFilterRequested(object? sender, EventArgs e)
        {
            ShowFilterView();
        }
    }
}
