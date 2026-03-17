using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VolleyStats.Data;
using VolleyStats.Models;

namespace VolleyStats.ViewModels
{
    public enum ReportMode
    {
        SingleMatch,
        TeamAggregate
    }

    public partial class MatchReportViewModel : ViewModelBase
    {
        private readonly IReadOnlyList<string> _matchFilePaths;
        private readonly string _analysisTeam;
        private readonly DvwFileParser _parser = new();
        private readonly Func<Task>? _navigateBack;

        /// <summary>Which file to use for single-match mode. Null = use first.</summary>
        private readonly string? _singleMatchFile;
        private readonly ReportMode _mode;

        [ObservableProperty] private string _title = "Match Report";
        [ObservableProperty] private string _subtitle = "";
        [ObservableProperty] private bool _isLoading = true;
        [ObservableProperty] private string _statusMessage = "";
        [ObservableProperty] private bool _isEmbedded;
        [ObservableProperty] private bool _isTeamOnly;

        // Match info
        [ObservableProperty] private string _league = "";
        [ObservableProperty] private string _matchDate = "";
        [ObservableProperty] private string _venue = "";
        [ObservableProperty] private string _referees = "";
        [ObservableProperty] private string _homeTeamName = "";
        [ObservableProperty] private string _awayTeamName = "";
        [ObservableProperty] private int _homeSetsWon;
        [ObservableProperty] private int _awaySetsWon;

        // Set scores
        public ObservableCollection<SetScoreRow> SetScores { get; } = new();

        // Player stats
        public ObservableCollection<PlayerStatsRow> HomePlayerStats { get; } = new();
        public ObservableCollection<PlayerStatsRow> AwayPlayerStats { get; } = new();

        // Team totals
        [ObservableProperty] private TeamTotalRow? _homeTotals;
        [ObservableProperty] private TeamTotalRow? _awayTotals;

        // Per-set team stats
        public ObservableCollection<SetTeamStatsRow> HomeSetStats { get; } = new();
        public ObservableCollection<SetTeamStatsRow> AwaySetStats { get; } = new();

        // Coaches
        [ObservableProperty] private string _homeCoach = "";
        [ObservableProperty] private string _awayCoach = "";

        private MatchReportData? _reportData;

        public IRelayCommand BackCommand { get; }
        public IAsyncRelayCommand ExportPdfCommand { get; }

        public MatchReportViewModel(
            IReadOnlyList<string> matchFilePaths,
            string analysisTeam,
            Func<Task>? navigateBack,
            ReportMode mode = ReportMode.SingleMatch,
            string? singleMatchFile = null)
        {
            _matchFilePaths = matchFilePaths;
            _analysisTeam = analysisTeam;
            _navigateBack = navigateBack;
            _mode = mode;
            _singleMatchFile = singleMatchFile;
            Subtitle = analysisTeam;

            BackCommand = new AsyncRelayCommand(navigateBack ?? (() => Task.CompletedTask));
            ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);
        }

        public async Task InitializeAsync()
        {
            IsLoading = true;
            StatusMessage = "Loading match data...";

            await Task.Run(() =>
            {
                if (_matchFilePaths.Count == 0) return;

                if (_mode == ReportMode.TeamAggregate)
                {
                    var matches = new List<Match>();
                    foreach (var fp in _matchFilePaths)
                    {
                        try { matches.Add(_parser.ParseDvwFile(fp)); }
                        catch { /* skip unreadable files */ }
                    }
                    if (matches.Count > 0)
                        _reportData = MatchStatisticsCalculator.CalculateTeamAggregate(matches, _analysisTeam);
                }
                else
                {
                    var filePath = _singleMatchFile ?? _matchFilePaths[0];
                    var match = _parser.ParseDvwFile(filePath);
                    _reportData = MatchStatisticsCalculator.Calculate(match);
                }
            });

            if (_reportData != null)
            {
                IsTeamOnly = _reportData.IsTeamOnly;
                PopulateFromData(_reportData);
            }

            IsLoading = false;
            StatusMessage = "";
        }

        private void PopulateFromData(MatchReportData data)
        {
            var match = data.Match;

            League = $"{match.Info?.League} {match.Info?.Phase}".Trim();
            Referees = match.MoreInfo?.Referees ?? "";

            if (data.IsTeamOnly)
            {
                HomeTeamName = data.HomeStats.TeamName;
                AwayTeamName = "";
                Title = $"{HomeTeamName} — {data.MatchesAggregated} match(es)";
                MatchDate = $"{data.MatchesAggregated} matches aggregated";
                Venue = "";
            }
            else
            {
                MatchDate = match.Info?.Date.ToString("MM/dd/yyyy") ?? "";
                Venue = $"{match.MoreInfo?.City} - {match.MoreInfo?.Hall}".Trim(' ', '-');
                HomeTeamName = match.HomeTeam.Name;
                AwayTeamName = match.AwayTeam.Name;
                HomeSetsWon = match.HomeTeam.SetsWon;
                AwaySetsWon = match.AwayTeam.SetsWon;
                Title = $"{HomeTeamName} vs {AwayTeamName}";

                // Set scores
                if (match.Sets != null)
                {
                    for (int i = 0; i < match.Sets.Count; i++)
                    {
                        var set = match.Sets[i];
                        var partials = set.PartialScores ?? new List<MatchScore>();
                        SetScores.Add(new SetScoreRow
                        {
                            SetNumber = i + 1,
                            Duration = $"0:{set.Duration:D2}",
                            Partial1 = partials.Count > 0 ? $"{partials[0].HomeTeamScore}-{partials[0].AwayTeamScore}" : "",
                            Partial2 = partials.Count > 1 ? $"{partials[1].HomeTeamScore}-{partials[1].AwayTeamScore}" : "",
                            Partial3 = partials.Count > 2 ? $"{partials[2].HomeTeamScore}-{partials[2].AwayTeamScore}" : "",
                            FinalScore = set.FinalScore != null ? $"{set.FinalScore.HomeTeamScore}-{set.FinalScore.AwayTeamScore}" : ""
                        });
                    }
                }
            }

            // Home (or only) team players
            PopulatePlayerStats(data.HomeStats, HomePlayerStats);
            HomeTotals = CreateTotalRow(data.HomeStats);
            HomeCoach = FormatCoach(data.HomeStats);

            foreach (var ss in data.HomeStats.SetStats)
                HomeSetStats.Add(CreateSetStatsRow(ss));

            // Away team (only in single-match mode)
            if (!data.IsTeamOnly)
            {
                PopulatePlayerStats(data.AwayStats, AwayPlayerStats);
                AwayTotals = CreateTotalRow(data.AwayStats);
                AwayCoach = FormatCoach(data.AwayStats);

                foreach (var ss in data.AwayStats.SetStats)
                    AwaySetStats.Add(CreateSetStatsRow(ss));
            }
        }

        private static void PopulatePlayerStats(TeamStats ts, ObservableCollection<PlayerStatsRow> target)
        {
            foreach (var ps in ts.Players.OrderByDescending(p => p.SetsPlayed.Count).ThenBy(p => p.JerseyNumber))
            {
                target.Add(new PlayerStatsRow
                {
                    JerseyNumber = ps.JerseyNumber,
                    IsLibero = ps.IsLibero,
                    Name = ps.Name,
                    SetsPlayed = string.Join(",", ps.SetsPlayed),

                    ServeTot = Fmt(ps.ServeTot),
                    ServeErr = Fmt(ps.ServeErr),
                    ServePts = Fmt(ps.ServePts),

                    RecTot = Fmt(ps.RecTot),
                    RecErr = Fmt(ps.RecErr),
                    RecPosPercent = ps.RecPosPercent,
                    RecExcPercent = ps.RecExcPercent,

                    AtkTot = Fmt(ps.AtkTot),
                    AtkErr = Fmt(ps.AtkErr),
                    AtkBlo = Fmt(ps.AtkBlo),
                    AtkPts = Fmt(ps.AtkPts),
                    AtkPtsPercent = ps.AtkPtsPercent,

                    BlkPts = Fmt(ps.BlkPts),
                    TotalPoints = Fmt(ps.TotalPoints),
                });
            }
        }

        private static TeamTotalRow CreateTotalRow(TeamStats ts)
        {
            return new TeamTotalRow
            {
                ServeTot = ts.ServeTot.ToString(),
                ServeErr = ts.ServeErr.ToString(),
                ServePts = ts.ServePts.ToString(),
                RecTot = ts.RecTot.ToString(),
                RecErr = ts.RecErr.ToString(),
                RecPosPercent = ts.RecPosPercent,
                RecExcPercent = ts.RecExcPercent,
                AtkTot = ts.AtkTot.ToString(),
                AtkErr = ts.AtkErr.ToString(),
                AtkBlo = ts.AtkBlo.ToString(),
                AtkPts = ts.AtkPts.ToString(),
                AtkPtsPercent = ts.AtkPtsPercent,
                BlkPts = ts.BlkPts.ToString(),
                TotalPoints = (ts.ServePts + ts.AtkPts + ts.BlkPts).ToString(),
            };
        }

        private static SetTeamStatsRow CreateSetStatsRow(SetTeamStats ss)
        {
            return new SetTeamStatsRow
            {
                SetLabel = $"Set {ss.SetNumber}",
                ServeTot = Fmt(ss.ServeTot),
                ServeErr = Fmt(ss.ServeErr),
                ServePts = Fmt(ss.ServePts),
                RecTot = Fmt(ss.RecTot),
                RecErr = Fmt(ss.RecErr),
                RecPosPercent = ss.RecTot > 0 ? $"{ss.RecPos * 100 / ss.RecTot}%" : ".",
                RecExcPercent = ss.RecTot > 0 && ss.RecExc > 0 ? $"({ss.RecExc * 100 / ss.RecTot}%)" : ".",
                AtkTot = Fmt(ss.AtkTot),
                AtkErr = Fmt(ss.AtkErr),
                AtkBlo = Fmt(ss.AtkBlo),
                AtkPts = Fmt(ss.AtkPts),
                AtkPtsPercent = ss.AtkTot > 0 ? $"{ss.AtkPts * 100 / ss.AtkTot}%" : ".",
                BlkPts = Fmt(ss.BlkPts),
            };
        }

        private static string FormatCoach(TeamStats ts)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(ts.CoachName)) parts.Add($"Coach: {ts.CoachName}");
            if (!string.IsNullOrEmpty(ts.AssistantCoach)) parts.Add($"Assistant: {ts.AssistantCoach}");
            return string.Join("  |  ", parts);
        }

        private static string Fmt(int v) => v > 0 ? v.ToString() : ".";

        private async Task ExportPdfAsync()
        {
            if (_reportData == null) return;

            StatusMessage = "Exporting PDF...";

            var saveDialog = new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Export Match Report",
                SuggestedFileName = IsTeamOnly
                    ? $"TeamReport_{HomeTeamName}.pdf"
                    : $"MatchReport_{HomeTeamName}_vs_{AwayTeamName}.pdf",
                DefaultExtension = "pdf",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } }
                }
            };

            // Request save path from view via event
            var path = await RequestSavePath?.Invoke(saveDialog)!;

            if (!string.IsNullOrEmpty(path))
            {
                await Task.Run(() => MatchReportPdfGenerator.Generate(_reportData, path));
                StatusMessage = $"Exported to {System.IO.Path.GetFileName(path)}";
            }
            else
            {
                StatusMessage = "";
            }
        }

        // Event for the View to handle the save dialog
        public Func<Avalonia.Platform.Storage.FilePickerSaveOptions, Task<string?>>? RequestSavePath { get; set; }
    }

    public class SetScoreRow
    {
        public int SetNumber { get; set; }
        public string Duration { get; set; } = "";
        public string Partial1 { get; set; } = "";
        public string Partial2 { get; set; } = "";
        public string Partial3 { get; set; } = "";
        public string FinalScore { get; set; } = "";
    }

    public class PlayerStatsRow
    {
        public int JerseyNumber { get; set; }
        public bool IsLibero { get; set; }
        public string Name { get; set; } = "";
        public string SetsPlayed { get; set; } = "";

        public string ServeTot { get; set; } = ".";
        public string ServeErr { get; set; } = ".";
        public string ServePts { get; set; } = ".";

        public string RecTot { get; set; } = ".";
        public string RecErr { get; set; } = ".";
        public string RecPosPercent { get; set; } = ".";
        public string RecExcPercent { get; set; } = ".";

        public string AtkTot { get; set; } = ".";
        public string AtkErr { get; set; } = ".";
        public string AtkBlo { get; set; } = ".";
        public string AtkPts { get; set; } = ".";
        public string AtkPtsPercent { get; set; } = ".";

        public string BlkPts { get; set; } = ".";
        public string TotalPoints { get; set; } = ".";
    }

    public class TeamTotalRow
    {
        public string ServeTot { get; set; } = "0";
        public string ServeErr { get; set; } = "0";
        public string ServePts { get; set; } = "0";

        public string RecTot { get; set; } = "0";
        public string RecErr { get; set; } = "0";
        public string RecPosPercent { get; set; } = ".";
        public string RecExcPercent { get; set; } = ".";

        public string AtkTot { get; set; } = "0";
        public string AtkErr { get; set; } = "0";
        public string AtkBlo { get; set; } = "0";
        public string AtkPts { get; set; } = "0";
        public string AtkPtsPercent { get; set; } = ".";

        public string BlkPts { get; set; } = "0";
        public string TotalPoints { get; set; } = "0";
    }

    public class SetTeamStatsRow
    {
        public string SetLabel { get; set; } = "";
        public string ServeTot { get; set; } = ".";
        public string ServeErr { get; set; } = ".";
        public string ServePts { get; set; } = ".";

        public string RecTot { get; set; } = ".";
        public string RecErr { get; set; } = ".";
        public string RecPosPercent { get; set; } = ".";
        public string RecExcPercent { get; set; } = ".";

        public string AtkTot { get; set; } = ".";
        public string AtkErr { get; set; } = ".";
        public string AtkBlo { get; set; } = ".";
        public string AtkPts { get; set; } = ".";
        public string AtkPtsPercent { get; set; } = ".";

        public string BlkPts { get; set; } = ".";
    }
}
