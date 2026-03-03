using Avalonia.Controls;
using VolleyStats.Data;
using VolleyStats.Data.Repositories;
using VolleyStats.ViewModels;

namespace VolleyStats.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            var matchSummaryLoader = new MatchSummaryLoader();
            var teamsRepository = new TeamsRepository();

            _viewModel = new MainWindowViewModel(matchSummaryLoader, teamsRepository);
            DataContext = _viewModel;

            Opened += async (_, _) => await _viewModel.InitializeAsync();
        }
    }
}