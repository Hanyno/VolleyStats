using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using VolleyStats.Data;
using VolleyStats.Domain;
using VolleyStats.DTO;
using VolleyStats.Enums;

namespace VolleyStats.Services
{
    public class TeamAnalysisService : ITeamAnalysisService
    {
        private readonly OfficialMatchRepository _matchRepository;

        public TeamAnalysisService(OfficialMatchRepository matchRepository)
        {
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
        }

        public TeamBasicOverviewDto GetBasicOverview(
            int teamId,
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            int? competitionId = null,
            bool includeHome = true,
            bool includeAway = true,
            int? limitLastMatches = null)
        {
            var dto = new TeamBasicOverviewDto();

            var matches = _matchRepository.GetFinishedMatchesForTeam(
                teamId,
                fromUtc,
                toUtc,
                competitionId,
                includeHome,
                includeAway,
                limitLastMatches);

            foreach (var match in matches)
            {
                _matchRepository.LoadMatchStatistics(match);

                dto.MatchesPlayed++;

                bool isHomeTeam = match.HomeTeam.Id == teamId;

                int teamSetsInMatch = 0;
                int oppSetsInMatch = 0;

                foreach (var set in match.Sets)
                {
                    int teamScore = isHomeTeam ? set.HomeScore : set.AwayScore;
                    int oppScore = isHomeTeam ? set.AwayScore : set.HomeScore;

                    if (teamScore > oppScore)
                    {
                        dto.SetsWon++;
                        teamSetsInMatch++;
                    }
                    else if (oppScore > teamScore)
                    {
                        dto.SetsLost++;
                        oppSetsInMatch++;
                    }
                }

                if (teamSetsInMatch > oppSetsInMatch)
                {
                    dto.MatchesWon++;
                }
            }

            return dto;
        }

        public SkillAnalysisDto GetSkillAnalysis(
            int teamId,
            BasicSkill skill,
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            int? competitionId = null,
            bool includeHome = true,
            bool includeAway = true,
            int? limitLastMatches = null)
        {
            var result = new SkillAnalysisDto
            {
                SkillName = skill.ToString()
            };

            var matches = _matchRepository.GetFinishedMatchesForTeam(
                teamId,
                fromUtc,
                toUtc,
                competitionId,
                includeHome,
                includeAway,
                limitLastMatches);

            var orderedMatches = matches
                .OrderBy(m => m.StartTime)
                .ToList();

            foreach (var match in orderedMatches)
            {
                _matchRepository.LoadMatchStatistics(match);

                bool isHomeTeam = match.HomeTeam.Id == teamId;
                var teamSide = isHomeTeam ? TeamSide.Home : TeamSide.Away;

                int attempts = 0;
                int points = 0;
                int errors = 0;

                foreach (var set in match.Sets)
                {
                    foreach (var rally in set.Rallies)
                    {
                        foreach (var ev in rally.Events)
                        {
                            if (ev.Side != teamSide)
                                continue;

                            if (ev.Skill != skill)
                                continue;

                            attempts++;

                            if (skill == BasicSkill.Reception)
                            {
                                if (IsReceptionPositive(ev.Eval))
                                {
                                    points++;
                                }

                                if (IsReceptionNegative(ev.Eval))
                                {
                                    errors++;
                                }
                            }
                            else
                            {
                                if (IsPoint(skill, ev.Eval))
                                    points++;

                                if (IsError(skill, ev.Eval))
                                    errors++;
                            }
                        }
                    }
                }

                var opponentName = isHomeTeam
                    ? match.AwayTeam.Name
                    : match.HomeTeam.Name;

                result.Trend.Add(new SkillTrendPointDto
                {
                    MatchDate = match.StartTime,
                    OpponentName = opponentName,
                    Attempts = attempts,
                    Points = points,
                    Errors = errors
                });
            }

            return result;
        }

        public IEnumerable<PlayerStatsDto> GetPlayersStats(
            int teamId,
            DateTime? fromUtc,
            DateTime? toUtc,
            int? competitionId,
            bool includeHome,
            bool includeAway,
            int? limitLastMatches)
        {
            var matches = _matchRepository.GetFinishedMatchesForTeam(
                teamId, fromUtc, toUtc, competitionId, includeHome, includeAway, limitLastMatches);

            var dict = new Dictionary<int, PlayerStatsDto>();

            foreach (var match in matches)
            {
                _matchRepository.LoadMatchStatistics(match);
                bool isHomeTeam = match.HomeTeam.Id == teamId;
                var players = isHomeTeam
                    ? match.HomeTeam.Players
                    : match.AwayTeam.Players;

                foreach (var p in players)
                {
                    if (!dict.ContainsKey(p.Id))
                    {
                        dict[p.Id] = new PlayerStatsDto
                        {
                            JerseyNumber = p.JerseyNumber,
                            PlayerName = $"{p.FirstName} {p.LastName}"
                        };
                    }
                }

                var teamSide = isHomeTeam ? TeamSide.Home : TeamSide.Away;

                foreach (var set in match.Sets)
                {
                    foreach (var rally in set.Rallies)
                    {
                        foreach (var ev in rally.Events)
                        {
                            if (ev.Side != teamSide)
                            {
                                continue;
                            }
                            if (!ev.PlayerId.HasValue)
                                continue;

                            var playerId = ev.PlayerId.Value;

                            if (!dict.TryGetValue(playerId, out var stats))
                                continue;


                            switch (ev.Skill)
                            {
                                case BasicSkill.Attack:
                                    stats.AttackAttempts++;
                                    if (ev.Eval == EvaluationSymbol.Point) stats.AttackPoints++;
                                    if (ev.Eval == EvaluationSymbol.Error || ev.Eval == EvaluationSymbol.Over)
                                        stats.AttackErrors++;
                                    break;

                                case BasicSkill.Serve:
                                    stats.ServeAttempts++;
                                    if (ev.Eval == EvaluationSymbol.Point) stats.ServePoints++;
                                    if (ev.Eval == EvaluationSymbol.Error) stats.ServeErrors++;
                                    break;

                                case BasicSkill.Reception:
                                    stats.ReceptionAttempts++;

                                    if (ev.Eval == EvaluationSymbol.Point || ev.Eval == EvaluationSymbol.Positive)
                                        stats.ReceptionPositive++;

                                    if (ev.Eval == EvaluationSymbol.Good || ev.Eval == EvaluationSymbol.Poor)
                                        stats.ReceptionNegative++;

                                    if (ev.Eval == EvaluationSymbol.Error)
                                        stats.ReceptionErrors++;
                                    break;
                            }
                        }
                    }
                }
            }

            foreach (var s in dict.Values)
            {
                s.AttackEfficiency = s.AttackAttempts > 0
                    ? (double)(s.AttackPoints - s.AttackErrors) / s.AttackAttempts
                    : 0;

                s.ServeEfficiency = s.ServeAttempts > 0
                    ? (double)(s.ServePoints - s.ServeErrors) / s.ServeAttempts
                    : 0;

                s.ReceptionEfficiency = s.ReceptionAttempts > 0
                    ? (double)(s.ReceptionPositive - s.ReceptionErrors) / s.ReceptionAttempts
                    : 0;
            }

            return dict.Values.OrderBy(s => s.JerseyNumber).ToList();
        }


        private static bool IsPoint(BasicSkill skill, EvaluationSymbol eval)
        {
            switch (skill)
            {
                case BasicSkill.Attack:
                    // Attack:
                    // # point
                    // / Blocked
                    // = Error
                    return eval == EvaluationSymbol.Point;

                case BasicSkill.Block:
                    // Block:
                    // # point
                    // / Block Error
                    // = Block Error
                    return eval == EvaluationSymbol.Point;

                case BasicSkill.Serve:
                    // Serve:
                    // # Point
                    // = error
                    return eval == EvaluationSymbol.Point;

                default:
                    return false;
            }
        }

        private static bool IsError(BasicSkill skill, EvaluationSymbol eval)
        {
            switch (skill)
            {
                case BasicSkill.Attack:
                    // Attack:
                    // # point
                    // / Blocked 
                    // = Error
                    return eval == EvaluationSymbol.Error
                           || eval == EvaluationSymbol.Over;

                case BasicSkill.Block:
                    // Block:
                    // # point
                    // / Block Error 
                    // = Block Error 
                    return eval == EvaluationSymbol.Error
                           || eval == EvaluationSymbol.Over;

                case BasicSkill.Serve:
                    // Serve:
                    // # Point
                    // = error
                    return eval == EvaluationSymbol.Error;

                default:
                    return false;
            }
        }

        private static bool IsReceptionPositive(EvaluationSymbol eval)
        {
            // Reception:
            // # Perfect
            // + good
            return eval == EvaluationSymbol.Point   // '#'
                   || eval == EvaluationSymbol.Positive; // '+'
        }
        private static bool IsReceptionNegative(EvaluationSymbol eval)
        {
            // Reception:
            // !- negative
            // = error
            return eval == EvaluationSymbol.Error   // '='
                   || eval == EvaluationSymbol.Good   // '!'
                   || eval == EvaluationSymbol.Poor;  // '-'
        }
    }
}
