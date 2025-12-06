using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Globalization;
using VolleyStats.Data;
using VolleyStats.Domain;
using VolleyStats.Services;

namespace VolleyStats.Views
{
    public partial class CreationMatchWindow : Window
    {
        private readonly IOfficialStatsService _officialStatsService;

        private List<Team> _teams = new();

        public CreationMatchWindow(IOfficialStatsService officialStatsService)
        {
            _officialStatsService = officialStatsService;

            InitializeComponent();


            LoadTeams();
        }

        private void LoadTeams()
        {
            _teams = _officialStatsService.GetAllTeams();

            HomeTeamComboBox.ItemsSource = _teams;
            AwayTeamComboBox.ItemsSource = _teams;
        }

        private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (MatchDatePicker.SelectedDate is null)
            {
                ShowSimpleMessage("Please select date.");
                return;
            }

            if (string.IsNullOrWhiteSpace(MatchTimeTextBox.Text))
            {
                ShowSimpleMessage("Please enter time in format HH:mm.");
                return;
            }

            if (HomeTeamComboBox.SelectedItem is not Team home)
            {
                ShowSimpleMessage("Please select home team.");
                return;
            }

            if (AwayTeamComboBox.SelectedItem is not Team away)
            {
                ShowSimpleMessage("Please select away team.");
                return;
            }

            if (home.Id == away.Id)
            {
                ShowSimpleMessage("Home and away team cannot be the same.");
                return;
            }

            if (!DateTime.TryParseExact(
                    MatchTimeTextBox.Text.Trim(),
                    "HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var timePart))
            {
                ShowSimpleMessage("Invalid time format. Use HH:mm.");
                return;
            }

            var dateOnly = MatchDatePicker.SelectedDate.Value;
            var dateTime = new DateTime(
                dateOnly.Year,
                dateOnly.Month,
                dateOnly.Day,
                timePart.Hour,
                timePart.Minute,
                0,
                DateTimeKind.Local);

            var match = new Match
            {
                StartTime = dateTime,
                HomeTeam = home,
                AwayTeam = away,
                MatchCode = "",
                Season = "",
                CompetitionCode = ""
            };

            _officialStatsService.UploadMatches(new[] {match});

            Close(match);
        }

        private async void ShowSimpleMessage(string message)
        {
            var dlg = new Window
            {
                Title = "Warning",
                Width = 300,
                Height = 150,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(10),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Margin = new Avalonia.Thickness(0,0,0,10)
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Width = 80
                        }
                    }
                }
            };

            await dlg.ShowDialog(this);
        }
    }
}
