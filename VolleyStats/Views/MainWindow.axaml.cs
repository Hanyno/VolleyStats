using Avalonia.Controls;
using Avalonia.Interactivity;
using VolleyStats.Data;
using VolleyStats.Data.Repositories;
using VolleyStats.Domain;
using VolleyStats.ViewModels;

namespace VolleyStats.Views
{
    public partial class MainWindow : Window
    {
        private readonly TeamsRepository _teamsRepository = new();
        private readonly MatchSummaryLoader _matchSummaryLoader = new();
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainWindowViewModel(_matchSummaryLoader, _teamsRepository);
            DataContext = _viewModel;

            Opened += async (_, _) => await _viewModel.InitializeAsync();
        }

        private async void TeamsButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var teamsWindow = new TeamsWindow();
            var viewModel = new TeamsViewModel(_teamsRepository, teamsWindow, teamsWindow);

            teamsWindow.SetViewModel(viewModel);

            await viewModel.LoadTeamsCommand.ExecuteAsync(null);
            await teamsWindow.ShowDialog(this);

            await _viewModel.InitializeAsync();
        }

        private async void SelectTeamButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var picker = new TeamPickerWindow();
            var pickerViewModel = new TeamPickerViewModel(_teamsRepository);
            picker.DataContext = pickerViewModel;
            await pickerViewModel.LoadTeamsAsync();

            var selected = await picker.ShowDialog<Team?>(this);
            _viewModel.ApplyTeamFilter(selected?.Name);
        }

        private async void ClearTeamFilterButton_OnClick(object? sender, RoutedEventArgs e)
        {
            _viewModel.ApplyTeamFilter(null);
            await _viewModel.LoadMatchesAsync();
        }
    }
}