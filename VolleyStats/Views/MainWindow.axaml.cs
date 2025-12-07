using Avalonia.Controls;
using Avalonia.Interactivity;
using VolleyStats.Services;
using VolleyStats.Domain;

namespace VolleyStats.Views
{
    public partial class MainWindow : Window
    {
        private readonly ITeamsService _teamsService;
        private readonly IOfficialStatsService _officialStatsService;
        private readonly ITeamAnalysisService _teamAnalysisService;
        public MainWindow(ITeamsService teamsService, IOfficialStatsService officialStatsService, ITeamAnalysisService teamAnalysisService)
        {
            _teamsService = teamsService;
            _officialStatsService = officialStatsService;
            _teamAnalysisService = teamAnalysisService;

            InitializeComponent();
            ShowHome();
        }
        public void ShowScouting(Match match)
        {
            MainContent.Content = new ScoutingView(match, _officialStatsService);
        }

        public void ShowHome()
        {
            MainContent.Content = new HomeView(_teamsService, _officialStatsService, _teamAnalysisService);
        }

        public void ShowTeamAnalysis()
        {
            MainContent.Content = new TeamAnalysisView(_teamAnalysisService, _officialStatsService);
        }
    }
}