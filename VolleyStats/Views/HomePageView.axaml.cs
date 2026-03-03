using Avalonia.Controls;
using Avalonia.Interactivity;
using VolleyStats.Data.Repositories;
using VolleyStats.Domain;
using VolleyStats.ViewModels;

namespace VolleyStats.Views
{
    public partial class HomePageView : UserControl
    {
        private readonly TeamsRepository _teamsRepository = new();

        public HomePageView()
        {
            InitializeComponent();
        }

        private HomePageViewModel? ViewModel => DataContext as HomePageViewModel;

        private Window? GetParentWindow()
        {
            return TopLevel.GetTopLevel(this) as Window;
        }

        private async void TeamsButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var parentWindow = GetParentWindow();
            if (parentWindow == null) return;

            var teamsWindow = new TeamsWindow();
            var viewModel = new TeamsViewModel(_teamsRepository, teamsWindow, teamsWindow);

            teamsWindow.SetViewModel(viewModel);

            await viewModel.LoadTeamsCommand.ExecuteAsync(null);
            await teamsWindow.ShowDialog(parentWindow);

            if (ViewModel != null)
                await ViewModel.InitializeAsync();
        }

        private async void SelectTeamButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var parentWindow = GetParentWindow();
            if (parentWindow == null || ViewModel == null) return;

            var picker = new TeamPickerWindow();
            var pickerViewModel = new TeamPickerViewModel(_teamsRepository);
            picker.DataContext = pickerViewModel;
            await pickerViewModel.LoadTeamsAsync();

            var selected = await picker.ShowDialog<Team?>(parentWindow);
            ViewModel.ApplyTeamFilter(selected?.Name);
        }

        private async void ClearTeamFilterButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;

            ViewModel.ApplyTeamFilter(null);
            await ViewModel.LoadMatchesAsync();
        }
    }
}
