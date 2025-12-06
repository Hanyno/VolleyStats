using Avalonia.Controls;
using Avalonia.Input;
using System.Collections.Generic;
using VolleyStats.Data;
using VolleyStats.Domain;
using VolleyStats.Services;

namespace VolleyStats.Views
{
    public partial class MatchesWindow : Window
    {
        private readonly IOfficialStatsService _officialStatsService;

        public MatchesWindow(IOfficialStatsService officialStatsService)
        {
            _officialStatsService = officialStatsService;

            InitializeComponent();

            LoadMatches();
        }

        private void LoadMatches()
        {
            IEnumerable<Match> matches = _officialStatsService.GetPlannedMatches();
            MatchesListBox.ItemsSource = matches;
        }

        private void MatchesListBox_OnDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (MatchesListBox.SelectedItem is not Match match)
                return;

            if (Owner is not MainWindow mainWindow)
                return;

            mainWindow.ShowScouting(match);

            Close();
        }
    }
}
