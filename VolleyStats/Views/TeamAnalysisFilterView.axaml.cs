using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using VolleyStats.Domain;

namespace VolleyStats.Views
{
    public class TeamAnalysisFilterEventArgs : EventArgs
    {
        public Team? Team { get; set; }
        public string? PeriodText { get; set; }
        public int? CompetitionId { get; set; }
        public int? OpponentTeamId { get; set; }
    }

    public partial class TeamAnalysisFilterView : UserControl
    {
        public event EventHandler<TeamAnalysisFilterEventArgs>? GenerateRequested;

        private List<Team> _teams = new();

        public TeamAnalysisFilterView()
        {
            InitializeComponent();
        }

        public void Initialize(IEnumerable<Team> teams)
        {
            _teams = teams.ToList();

            TeamComboBox.ItemsSource = _teams;
            TeamComboBox.SelectedIndex = _teams.Count > 0 ? 0 : -1;

            PeriodComboBox.ItemsSource = new[]
                    {
                "Current season",
                "Last 5 matches",
                "All"
            };
            PeriodComboBox.SelectedIndex = 0;

        }


        private void GenerateButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var team = TeamComboBox.SelectedItem as Team;


            var args = new TeamAnalysisFilterEventArgs
            {
                Team = team,
                PeriodText = PeriodComboBox.SelectedItem as string,
            };

            GenerateRequested?.Invoke(this, args);
        }
    }
}
