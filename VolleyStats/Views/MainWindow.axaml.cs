using Avalonia.Controls;
using Avalonia.Interactivity;
using VolleyStats.Data.Repositories;
using VolleyStats.ViewModels;

namespace VolleyStats.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void TeamsButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var repository = new TeamsRepository();
            var teamsWindow = new TeamsWindow();
            var viewModel = new TeamsViewModel(repository, teamsWindow, teamsWindow);

            teamsWindow.SetViewModel(viewModel);

            await viewModel.LoadTeamsCommand.ExecuteAsync(null);
            await teamsWindow.ShowDialog(this);
        }
    }
}