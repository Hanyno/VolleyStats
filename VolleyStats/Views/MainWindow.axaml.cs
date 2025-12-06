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
        public MainWindow(ITeamsService teamsService, IOfficialStatsService officialStatsService)
        {
            _teamsService = teamsService;
            _officialStatsService = officialStatsService;

            InitializeComponent();
            ShowHome();
        }
        public void ShowScouting(Match match)
        {
            MainContent.Content = new ScoutingView(match, _officialStatsService);
        }

        public void ShowHome()
        {
            MainContent.Content = new HomeView(_teamsService, _officialStatsService);
        }


    }
}