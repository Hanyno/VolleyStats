using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VolleyStats.Enums;

namespace VolleyStats.Domain
{
    /// <summary>
    /// Kód změny skóre – např. "ap01:02", "*p02:02".
    /// Udává aktuální stav setu (HomeScore:AwayScore) a tým, který bod získal.
    /// </summary>
    public sealed class ScoreCode : Code
    {
        /// <summary>
        /// Tým, který získal tento bod.
        /// </summary>
        public TeamSide ScoringTeam { get; }

        /// <summary>
        /// Aktuální počet bodů domácího týmu v setu po tomto rally.
        /// </summary>
        public int HomeScore { get; }

        /// <summary>
        /// Aktuální počet bodů hostujícího týmu v setu po tomto rally.
        /// </summary>
        public int AwayScore { get; }

        public ScoreCode(
            string rawLine,
            string kod,
            char sp,
            char pr,
            CoordinatesPair? start,
            CoordinatesPair? middle,
            CoordinatesPair? end,
            DateTime? recordedAt,
            int? setNumber,
            int? homeSetterZone,
            int? awaySetterZone,
            string? videoFile,
            int? videoSecond,
            int[]? homeZones,
            int[]? awayZones)
            : base(rawLine,
                   kod,
                   sp,
                   pr,
                   start,
                   middle,
                   end,
                   recordedAt,
                   setNumber,
                   homeSetterZone,
                   awaySetterZone,
                   videoFile ?? string.Empty,
                   videoSecond,
                   homeZones ?? Array.Empty<int>(),
                   awayZones ?? Array.Empty<int>())
        {
            (ScoringTeam, HomeScore, AwayScore) = ParseScore(kod);
        }

        private static (TeamSide team, int home, int away) ParseScore(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
                return (TeamSide.Home, 0, 0);

            var s = kod.Trim();
            int idx = 0;

            // prefix – tým
            TeamSide team;
            if (s[idx] == '*')
            {
                team = TeamSide.Home;
                idx++;
            }
            else if (s[idx] == 'a' || s[idx] == 'A')
            {
                team = TeamSide.Away;
                idx++;
            }
            else
            {
                // fallback – kdyby tam prefix nebyl, bereme domácí
                team = TeamSide.Home;
            }

            // očekáváme 'p' / 'P'
            if (idx < s.Length && (s[idx] == 'p' || s[idx] == 'P'))
                idx++;

            int home = 0;
            int away = 0;

            var rest = s.Substring(idx);   // např. "01:02"
            var parts = rest.Split(':');

            if (parts.Length == 2)
            {
                // vedou/nulují se i s leading zeros (01, 02, ...)
                if (!int.TryParse(parts[0], out home))
                    home = 0;
                if (!int.TryParse(parts[1], out away))
                    away = 0;
            }

            return (team, home, away);
        }

        public override string ToString()
            => $"{GetType().Name}: {HomeScore}:{AwayScore} (Scored by {ScoringTeam}), " +
               $"Time={RecordedAt?.ToString("HH:mm:ss") ?? "null"}, Set={SetNumber}";
    }

}
