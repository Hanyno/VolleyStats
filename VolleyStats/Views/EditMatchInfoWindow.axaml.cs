using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using VolleyStats.Models;

namespace VolleyStats.Views
{
    public partial class EditMatchInfoWindow : Window
    {
        private MatchInfo? _info;
        private MatchMoreInfo? _moreInfo;

        public EditMatchInfoWindow()
        {
            InitializeComponent();
        }

        public EditMatchInfoWindow(MatchInfo info, MatchMoreInfo? moreInfo)
        {
            InitializeComponent();
            _info     = info;
            _moreInfo = moreInfo;

            DateBox.Text     = info.Date != default ? info.Date.ToString("yyyy-MM-dd") : string.Empty;
            TimeBox.Text     = info.Time != default ? info.Time.ToString("HH:mm")      : string.Empty;
            SeasonBox.Text   = info.Seasson  ?? string.Empty;
            LeagueBox.Text   = info.League   ?? string.Empty;
            PhaseBox.Text    = info.Phase    ?? string.Empty;
            CityBox.Text     = moreInfo?.City      ?? string.Empty;
            HallBox.Text     = moreInfo?.Hall      ?? string.Empty;
            RefereesBox.Text = moreInfo?.Referees  ?? string.Empty;
            ScoutBox.Text    = moreInfo?.Scout     ?? string.Empty;
        }

        private void CancelButton_OnClick(object? sender, RoutedEventArgs e) => Close(false);

        private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_info != null)
            {
                if (DateOnly.TryParse(DateBox.Text, out var d))  _info.Date    = d;
                if (TimeOnly.TryParse(TimeBox.Text, out var t))  _info.Time    = t;
                _info.Seasson = SeasonBox.Text ?? string.Empty;
                _info.League  = LeagueBox.Text ?? string.Empty;
                _info.Phase   = PhaseBox.Text  ?? string.Empty;
            }

            if (_moreInfo != null)
            {
                _moreInfo.City      = CityBox.Text     ?? string.Empty;
                _moreInfo.Hall      = HallBox.Text     ?? string.Empty;
                _moreInfo.Referees  = RefereesBox.Text ?? string.Empty;
                _moreInfo.Scout     = ScoutBox.Text    ?? string.Empty;
            }

            Close(true);
        }
    }
}
