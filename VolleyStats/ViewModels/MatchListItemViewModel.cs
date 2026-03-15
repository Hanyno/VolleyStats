using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using VolleyStats.Models;

namespace VolleyStats.ViewModels
{
    public class MatchListItemViewModel : ObservableObject
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public MatchListItemViewModel(MatchSummary summary)
        {
            FileName = summary.FileName;
            FilePath = summary.FilePath;
            Season = summary.Season;
            HomeTeam = summary.HomeTeam;
            AwayTeam = summary.AwayTeam;
            HomeSets = summary.HomeSets;
            AwaySets = summary.AwaySets;
            Date = summary.Date;
            Time = summary.Time;
            League = summary.League;
            Phase = summary.Phase;
        }

        public string FileName { get; }
        public string FilePath { get; }
        public string? Season { get; }
        public string HomeTeam { get; }
        public string AwayTeam { get; }
        public int? HomeSets { get; }
        public int? AwaySets { get; }
        public DateOnly? Date { get; }
        public TimeOnly? Time { get; }
        public string? League { get; }
        public string? Phase { get; }

        public string DateText => Date?.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture) ?? string.Empty;
        public string TimeText => Time?.ToString("HH:mm", CultureInfo.CurrentCulture) ?? string.Empty;
        public string HomeSetsText => HomeSets?.ToString(CultureInfo.CurrentCulture) ?? "-";
        public string AwaySetsText => AwaySets?.ToString(CultureInfo.CurrentCulture) ?? "-";
        public string ScoreText => $"{HomeSetsText} - {AwaySetsText}";
        public string LeagueText => League ?? string.Empty;
        public string PhaseText => Phase ?? string.Empty;
    }
}
