using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VolleyStats.Enums;

namespace VolleyStats.Domain
{
    public class MatchRecordingSession
    {
        private readonly Match _match;

        public MatchSet CurrentSet { get; private set; } = null!;
        public Rally? CurrentRally { get; private set; }

        public TeamSide ServingSide { get; private set; }

        public int HomeScore => CurrentSet.HomeScore;
        public int AwayScore => CurrentSet.AwayScore;

        public MatchRecordingSession(Match match)
        {
            _match = match;
        }

        public void StartSet(int setNumber, TeamSide firstServer)
        {
            var set = new MatchSet
            {
                Match = _match,
                Number = setNumber,
                HomeScore = 0,
                AwayScore = 0
            };

            _match.Sets.Add(set);
            CurrentSet = set;

            ServingSide = firstServer;
            CurrentRally = null;
        }

        public void StartNewRally()
        {
            if (CurrentRally != null)
                throw new InvalidOperationException("Previous rally not finished.");

            var rally = new Rally
            {
                Set = CurrentSet,
                SequenceNumber = CurrentSet.Rallies.Count + 1,
                ServingSide = ServingSide,
                HomeScoreBefore = CurrentSet.HomeScore,
                AwayScoreBefore = CurrentSet.AwayScore
            };

            CurrentSet.Rallies.Add(rally);
            CurrentRally = rally;
        }

        public MatchEvent AddEvent(
            TeamSide side,
            MatchPlayer player,
            BasicSkill skill,
            EvaluationSymbol eval,
            string? attackComb = null,
            string? setterCall = null,
            string? zone = null,
            string? extra = null,
            DateTime? realTime = null)
        {
            if (CurrentRally == null)
                throw new InvalidOperationException("No active rally.");

            var ev = new MatchEvent
            {
                Set = CurrentSet,
                Rally = CurrentRally,
                OrderInRally = CurrentRally.Events.Count + 1,
                Side = side,
                Player = player,
                Skill = skill,
                Eval = eval,
                AttackCombinationCode = attackComb,
                SetterCallCode = setterCall,
                AttackZoneCode = zone,
                ExtraFlags = extra,
                RealTime = realTime ?? DateTime.Now
            };

            CurrentRally.Events.Add(ev);
            return ev;
        }

        public void EndRally(TeamSide winnerSide)
        {
            if (CurrentRally == null)
                throw new InvalidOperationException("No active rally.");

            if (winnerSide == TeamSide.Home)
                CurrentSet.HomeScore++;
            else
                CurrentSet.AwayScore++;

            CurrentRally.HomeScoreAfter = CurrentSet.HomeScore;
            CurrentRally.AwayScoreAfter = CurrentSet.AwayScore;

            if (winnerSide != ServingSide)
            {
                ServingSide = winnerSide;

                // tady později:
                // if (winnerSide == TeamSide.Home) HomeRotation.Rotate();
                // else AwayRotation.Rotate();
            }

            CurrentRally = null;
        }

        public bool IsSetFinished(int pointsToWin, out TeamSide? winner)
        {
            winner = null;

            if (CurrentSet.HomeScore >= pointsToWin ||
                CurrentSet.AwayScore >= pointsToWin)
            {
                if (Math.Abs(CurrentSet.HomeScore - CurrentSet.AwayScore) >= 2)
                {
                    winner = CurrentSet.HomeScore > CurrentSet.AwayScore
                        ? TeamSide.Home
                        : TeamSide.Away;

                    return true;
                }
            }
            return false;
        }

        public void EndSet()
        {
            if (CurrentRally != null)
                throw new InvalidOperationException("Cannot end set with active rally.");

        }

        public bool IsMatchFinished(int setsToWin, out TeamSide? winner)
        {
            int homeSets = _match.Sets.Count(s => s.HomeScore > s.AwayScore);
            int awaySets = _match.Sets.Count(s => s.AwayScore > s.HomeScore);

            if (homeSets >= setsToWin)
            {
                winner = TeamSide.Home;
                return true;
            }
            if (awaySets >= setsToWin)
            {
                winner = TeamSide.Away;
                return true;
            }

            winner = null;
            return false;
        }

    }

}
