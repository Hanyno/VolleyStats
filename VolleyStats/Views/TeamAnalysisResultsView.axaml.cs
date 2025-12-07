using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using VolleyStats.Domain;
using VolleyStats.DTO;

namespace VolleyStats.Views
{
    public partial class TeamAnalysisResultsView : UserControl
    {
        public event EventHandler? ExportRequested;
        public event EventHandler? HomeRequested;
        public event EventHandler? FilterRequested;

        public TeamAnalysisResultsView()
        {
            InitializeComponent();
        }

        public void ShowOverview(Team team, TeamBasicOverviewDto overview)
        {
            TeamTitleTextBlock.Text = team.Name;

            MatchesSummaryTextBlock.Text =
                $"{overview.MatchesPlayed} (W:{overview.MatchesWon}, L:{overview.MatchesLost})";

            SetsSummaryTextBlock.Text =
                $"{overview.SetsWon}:{overview.SetsLost}";

            WinRateSummaryTextBlock.Text =
                $"Match Win Rate: {overview.MatchWinRate:P1}, Set Win Rate: {overview.SetWinRate:P1}";
        }

        public void ShowPlayersStatistics(System.Collections.IEnumerable playerStats)
        {
            PlayersStatsDataGrid.ItemsSource = playerStats;
        }

        public void ShowNoData()
        {
            TeamTitleTextBlock.Text = "(no data)";
        }

        public void ClearAll()
        {
            TeamTitleTextBlock.Text = "(not selected)";
            MatchesSummaryTextBlock.Text = "";
            SetsSummaryTextBlock.Text = "";
            WinRateSummaryTextBlock.Text = "";

            PlayersStatsDataGrid.ItemsSource = null;
        }

        private void ExportButton_OnClick(object? sender, RoutedEventArgs e)
        {
            ExportRequested?.Invoke(this, EventArgs.Empty);
        }

        private void HomeButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (VisualRoot is MainWindow mainWindow)
            {
                mainWindow.ShowHome();
            }
        }

        private void BackToFilterButton_OnClick(object? sender, RoutedEventArgs e)
        {
            FilterRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
