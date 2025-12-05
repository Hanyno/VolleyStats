using Avalonia.Controls;
using Avalonia.Interactivity;
using VolleyStats.Services;

namespace VolleyStats.Views
{
    public partial class MainWindow : Window
    {
        private readonly ITeamsService _teamsService;

        public MainWindow(ITeamsService teamsService)
        {
            _teamsService = teamsService;

            InitializeComponent();
        }


        private void TeamsButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var win = new TeamsWindow(_teamsService);
            win.ShowDialog(this);
        }
    }
}