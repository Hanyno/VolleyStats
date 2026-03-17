using System;
using System.Collections.Generic;
using System.Linq;
using VolleyStats.Enums;
using VolleyStats.Models;

namespace VolleyStats.Data
{
    public class PlayerStats
    {
        public int JerseyNumber { get; set; }
        public string Name { get; set; } = "";
        public bool IsLibero { get; set; }
        public List<int> SetsPlayed { get; set; } = new();

        // Serve
        public int ServeTot { get; set; }
        public int ServeErr { get; set; }
        public int ServePts { get; set; }

        // Reception
        public int RecTot { get; set; }
        public int RecErr { get; set; }
        public int RecPos { get; set; }  // + and #
        public int RecExc { get; set; }  // # only

        // Attack
        public int AtkTot { get; set; }
        public int AtkErr { get; set; }
        public int AtkBlo { get; set; }  // blocked = /
        public int AtkPts { get; set; }

        // Block
        public int BlkPts { get; set; }

        // Points summary
        public int TotalPoints => ServePts + AtkPts + BlkPts;
        public int BreakPoints { get; set; }
        public int WonLost { get; set; }

        public double? Vote { get; set; }

        public string RecPosPercent => RecTot > 0 ? $"{RecPos * 100 / RecTot}%" : ".";
        public string RecExcPercent => RecTot > 0 && RecExc > 0 ? $"({RecExc * 100 / RecTot}%)" : ".";
        public string AtkPtsPercent => AtkTot > 0 ? $"{AtkPts * 100 / AtkTot}%" : ".";
    }

    public class TeamStats
    {
        public string TeamName { get; set; } = "";
        public string CoachName { get; set; } = "";
        public string AssistantCoach { get; set; } = "";
        public List<PlayerStats> Players { get; set; } = new();
        public List<SetTeamStats> SetStats { get; set; } = new();

        // Team totals
        public int TotalPoints { get; set; }
        public int TotalBreakPoints { get; set; }
        public int TotalWonLost { get; set; }

        public int ServeTot { get; set; }
        public int ServeErr { get; set; }
        public int ServePts { get; set; }

        public int RecTot { get; set; }
        public int RecErr { get; set; }
        public int RecPos { get; set; }
        public int RecExc { get; set; }

        public int AtkTot { get; set; }
        public int AtkErr { get; set; }
        public int AtkBlo { get; set; }
        public int AtkPts { get; set; }

        public int BlkPts { get; set; }
        public int OpponentErrors { get; set; }

        public string RecPosPercent => RecTot > 0 ? $"{RecPos * 100 / RecTot}%" : ".";
        public string RecExcPercent => RecTot > 0 && RecExc > 0 ? $"({RecExc * 100 / RecTot}%)" : ".";
        public string AtkPtsPercent => AtkTot > 0 ? $"{AtkPts * 100 / AtkTot}%" : ".";
    }

    public class SetTeamStats
    {
        public int SetNumber { get; set; }
        public int PointsWonServe { get; set; }
        public int PointsWonAttack { get; set; }
        public int PointsWonBlock { get; set; }
        public int PointsWonOpErr { get; set; }

        public int ServeTot { get; set; }
        public int ServeErr { get; set; }
        public int ServePts { get; set; }

        public int RecTot { get; set; }
        public int RecErr { get; set; }
        public int RecPos { get; set; }
        public int RecExc { get; set; }

        public int AtkTot { get; set; }
        public int AtkErr { get; set; }
        public int AtkBlo { get; set; }
        public int AtkPts { get; set; }

        public int BlkPts { get; set; }
    }

    public class MatchReportData
    {
        public Match Match { get; set; } = null!;
        public TeamStats HomeStats { get; set; } = new();
        public TeamStats AwayStats { get; set; } = new();

        /// <summary>True when this report shows only one team aggregated across matches.</summary>
        public bool IsTeamOnly { get; set; }
        public int MatchesAggregated { get; set; } = 1;
    }

    public static class MatchStatisticsCalculator
    {
        public static MatchReportData Calculate(Match match)
        {
            var data = new MatchReportData { Match = match };

            var ballCodes = match.ScoutCodes.OfType<BallContactCode>().ToList();
            var scoreCodes = match.ScoutCodes.OfType<CodeScoreMarker>().ToList();

            data.HomeStats = CalculateTeamStats(match, ballCodes, scoreCodes, TeamSide.Home);
            data.AwayStats = CalculateTeamStats(match, ballCodes, scoreCodes, TeamSide.Away);

            return data;
        }

        private static TeamStats CalculateTeamStats(
            Match match,
            List<BallContactCode> allBallCodes,
            List<CodeScoreMarker> allScoreCodes,
            TeamSide side)
        {
            bool isHome = side == TeamSide.Home;
            var team = isHome ? match.HomeTeam : match.AwayTeam;
            var players = isHome ? match.HomePlayers : match.AwayPlayers;
            var teamCodes = allBallCodes.Where(c => c.Team == side).ToList();
            var opponentCodes = allBallCodes.Where(c => c.Team != side && c.Team != TeamSide.Unknown).ToList();

            var stats = new TeamStats
            {
                TeamName = team.Name,
                CoachName = team.CoachName ?? "",
                AssistantCoach = team.AssistantCoachName ?? ""
            };

            int setCount = match.Sets?.Count ?? 0;

            // Build player stats
            foreach (var player in players)
            {
                var ps = new PlayerStats
                {
                    JerseyNumber = player.JerseyNumber,
                    Name = $"{player.LastName} {player.FirstName}".Trim(),
                    IsLibero = player.Position == PlayerPost.Libero,
                };

                // Determine which sets this player played
                for (int s = 1; s <= setCount; s++)
                {
                    var setPlayerCodes = teamCodes.Where(c => c.SetNumber == s && c.PlayerNumber == player.JerseyNumber);
                    if (setPlayerCodes.Any())
                        ps.SetsPlayed.Add(s);
                }

                var playerCodes = teamCodes.Where(c => c.PlayerNumber == player.JerseyNumber).ToList();

                // Serve
                var serves = playerCodes.Where(c => c.Skill == Skill.Serve).ToList();
                ps.ServeTot = serves.Count;
                ps.ServeErr = serves.Count(c => c.Evaluation == Evaluation.Error);
                ps.ServePts = serves.Count(c => c.Evaluation == Evaluation.Point);

                // Reception
                var recs = playerCodes.Where(c => c.Skill == Skill.Reception).ToList();
                ps.RecTot = recs.Count;
                ps.RecErr = recs.Count(c => c.Evaluation == Evaluation.Error);
                ps.RecPos = recs.Count(c => c.Evaluation == Evaluation.Positive || c.Evaluation == Evaluation.Point);
                ps.RecExc = recs.Count(c => c.Evaluation == Evaluation.Point);

                // Attack
                var attacks = playerCodes.Where(c => c.Skill == Skill.Attack).ToList();
                ps.AtkTot = attacks.Count;
                ps.AtkErr = attacks.Count(c => c.Evaluation == Evaluation.Error);
                ps.AtkBlo = attacks.Count(c => c.Evaluation == Evaluation.VeryPoorOrBlocked);
                ps.AtkPts = attacks.Count(c => c.Evaluation == Evaluation.Point);

                // Block
                var blocks = playerCodes.Where(c => c.Skill == Skill.Block).ToList();
                ps.BlkPts = blocks.Count(c => c.Evaluation == Evaluation.Point);

                stats.Players.Add(ps);
            }

            // Per-set stats
            for (int s = 1; s <= setCount; s++)
            {
                var setCodes = teamCodes.Where(c => c.SetNumber == s).ToList();
                var oppSetCodes = opponentCodes.Where(c => c.SetNumber == s).ToList();

                var setStats = new SetTeamStats { SetNumber = s };

                setStats.ServeTot = setCodes.Count(c => c.Skill == Skill.Serve);
                setStats.ServeErr = setCodes.Count(c => c.Skill == Skill.Serve && c.Evaluation == Evaluation.Error);
                setStats.ServePts = setCodes.Count(c => c.Skill == Skill.Serve && c.Evaluation == Evaluation.Point);

                setStats.RecTot = setCodes.Count(c => c.Skill == Skill.Reception);
                setStats.RecErr = setCodes.Count(c => c.Skill == Skill.Reception && c.Evaluation == Evaluation.Error);
                setStats.RecPos = setCodes.Count(c => c.Skill == Skill.Reception && (c.Evaluation == Evaluation.Positive || c.Evaluation == Evaluation.Point));
                setStats.RecExc = setCodes.Count(c => c.Skill == Skill.Reception && c.Evaluation == Evaluation.Point);

                setStats.AtkTot = setCodes.Count(c => c.Skill == Skill.Attack);
                setStats.AtkErr = setCodes.Count(c => c.Skill == Skill.Attack && c.Evaluation == Evaluation.Error);
                setStats.AtkBlo = setCodes.Count(c => c.Skill == Skill.Attack && c.Evaluation == Evaluation.VeryPoorOrBlocked);
                setStats.AtkPts = setCodes.Count(c => c.Skill == Skill.Attack && c.Evaluation == Evaluation.Point);

                setStats.BlkPts = setCodes.Count(c => c.Skill == Skill.Block && c.Evaluation == Evaluation.Point);

                // Opponent errors in this set count as our points
                setStats.PointsWonOpErr = oppSetCodes.Count(c => c.Evaluation == Evaluation.Error);

                setStats.PointsWonServe = setStats.ServePts;
                setStats.PointsWonAttack = setStats.AtkPts;
                setStats.PointsWonBlock = setStats.BlkPts;

                stats.SetStats.Add(setStats);
            }

            // Team totals
            stats.ServeTot = stats.Players.Sum(p => p.ServeTot);
            stats.ServeErr = stats.Players.Sum(p => p.ServeErr);
            stats.ServePts = stats.Players.Sum(p => p.ServePts);

            stats.RecTot = stats.Players.Sum(p => p.RecTot);
            stats.RecErr = stats.Players.Sum(p => p.RecErr);
            stats.RecPos = stats.Players.Sum(p => p.RecPos);
            stats.RecExc = stats.Players.Sum(p => p.RecExc);

            stats.AtkTot = stats.Players.Sum(p => p.AtkTot);
            stats.AtkErr = stats.Players.Sum(p => p.AtkErr);
            stats.AtkBlo = stats.Players.Sum(p => p.AtkBlo);
            stats.AtkPts = stats.Players.Sum(p => p.AtkPts);

            stats.BlkPts = stats.Players.Sum(p => p.BlkPts);

            stats.TotalPoints = stats.ServePts + stats.AtkPts + stats.BlkPts;
            stats.OpponentErrors = opponentCodes.Count(c => c.Evaluation == Evaluation.Error);
            stats.TotalBreakPoints = allScoreCodes
                .Where(c => c.Team == side)
                .Count(); // simplified

            stats.TotalWonLost = stats.TotalPoints - (stats.ServeErr + stats.AtkErr + stats.AtkBlo +
                teamCodes.Count(c => c.Skill == Skill.Reception && c.Evaluation == Evaluation.Error));

            return stats;
        }

        /// <summary>
        /// Aggregates stats for a single team across multiple matches.
        /// The team is identified by name (case-insensitive).
        /// Returns a MatchReportData with IsTeamOnly=true and only HomeStats populated.
        /// </summary>
        public static MatchReportData CalculateTeamAggregate(List<Match> matches, string teamName)
        {
            var aggregatedPlayers = new Dictionary<int, PlayerStats>(); // keyed by jersey number
            string coachName = "";
            string assistantCoach = "";

            foreach (var match in matches)
            {
                bool isHome = match.HomeTeam.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase);
                bool isAway = match.AwayTeam.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase);
                if (!isHome && !isAway) continue;

                var side = isHome ? TeamSide.Home : TeamSide.Away;
                var team = isHome ? match.HomeTeam : match.AwayTeam;
                var players = isHome ? match.HomePlayers : match.AwayPlayers;

                if (string.IsNullOrEmpty(coachName))
                    coachName = team.CoachName ?? "";
                if (string.IsNullOrEmpty(assistantCoach))
                    assistantCoach = team.AssistantCoachName ?? "";

                var ballCodes = match.ScoutCodes.OfType<BallContactCode>().ToList();
                var teamCodes = ballCodes.Where(c => c.Team == side).ToList();
                int setCount = match.Sets?.Count ?? 0;

                foreach (var player in players)
                {
                    if (!aggregatedPlayers.TryGetValue(player.JerseyNumber, out var ps))
                    {
                        ps = new PlayerStats
                        {
                            JerseyNumber = player.JerseyNumber,
                            Name = $"{player.LastName} {player.FirstName}".Trim(),
                            IsLibero = player.Position == PlayerPost.Libero,
                        };
                        aggregatedPlayers[player.JerseyNumber] = ps;
                    }

                    var playerCodes = teamCodes.Where(c => c.PlayerNumber == player.JerseyNumber).ToList();

                    // Track sets played (offset by match index doesn't matter, just count)
                    for (int s = 1; s <= setCount; s++)
                    {
                        if (teamCodes.Any(c => c.SetNumber == s && c.PlayerNumber == player.JerseyNumber))
                            ps.SetsPlayed.Add(s); // numbering will be sequential across matches
                    }

                    // Serve
                    var serves = playerCodes.Where(c => c.Skill == Skill.Serve).ToList();
                    ps.ServeTot += serves.Count;
                    ps.ServeErr += serves.Count(c => c.Evaluation == Evaluation.Error);
                    ps.ServePts += serves.Count(c => c.Evaluation == Evaluation.Point);

                    // Reception
                    var recs = playerCodes.Where(c => c.Skill == Skill.Reception).ToList();
                    ps.RecTot += recs.Count;
                    ps.RecErr += recs.Count(c => c.Evaluation == Evaluation.Error);
                    ps.RecPos += recs.Count(c => c.Evaluation == Evaluation.Positive || c.Evaluation == Evaluation.Point);
                    ps.RecExc += recs.Count(c => c.Evaluation == Evaluation.Point);

                    // Attack
                    var attacks = playerCodes.Where(c => c.Skill == Skill.Attack).ToList();
                    ps.AtkTot += attacks.Count;
                    ps.AtkErr += attacks.Count(c => c.Evaluation == Evaluation.Error);
                    ps.AtkBlo += attacks.Count(c => c.Evaluation == Evaluation.VeryPoorOrBlocked);
                    ps.AtkPts += attacks.Count(c => c.Evaluation == Evaluation.Point);

                    // Block
                    var blocks = playerCodes.Where(c => c.Skill == Skill.Block).ToList();
                    ps.BlkPts += blocks.Count(c => c.Evaluation == Evaluation.Point);
                }
            }

            var teamStats = new TeamStats
            {
                TeamName = teamName,
                CoachName = coachName,
                AssistantCoach = assistantCoach,
                Players = aggregatedPlayers.Values.ToList(),
            };

            teamStats.ServeTot = teamStats.Players.Sum(p => p.ServeTot);
            teamStats.ServeErr = teamStats.Players.Sum(p => p.ServeErr);
            teamStats.ServePts = teamStats.Players.Sum(p => p.ServePts);
            teamStats.RecTot = teamStats.Players.Sum(p => p.RecTot);
            teamStats.RecErr = teamStats.Players.Sum(p => p.RecErr);
            teamStats.RecPos = teamStats.Players.Sum(p => p.RecPos);
            teamStats.RecExc = teamStats.Players.Sum(p => p.RecExc);
            teamStats.AtkTot = teamStats.Players.Sum(p => p.AtkTot);
            teamStats.AtkErr = teamStats.Players.Sum(p => p.AtkErr);
            teamStats.AtkBlo = teamStats.Players.Sum(p => p.AtkBlo);
            teamStats.AtkPts = teamStats.Players.Sum(p => p.AtkPts);
            teamStats.BlkPts = teamStats.Players.Sum(p => p.BlkPts);
            teamStats.TotalPoints = teamStats.ServePts + teamStats.AtkPts + teamStats.BlkPts;

            // Use the first match as the "representative" for match info
            var firstMatch = matches.FirstOrDefault(m =>
                m.HomeTeam.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase) ||
                m.AwayTeam.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase)) ?? matches[0];

            return new MatchReportData
            {
                Match = firstMatch,
                HomeStats = teamStats,
                AwayStats = new TeamStats(),
                IsTeamOnly = true,
                MatchesAggregated = matches.Count
            };
        }
    }
}
