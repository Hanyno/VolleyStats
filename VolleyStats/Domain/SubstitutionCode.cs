using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VolleyStats.Enums;

namespace VolleyStats.Domain
{
    /// <summary>
    /// Střídání hráčů – kód typu "*c04:10" / "ac07:15".
    /// </summary>
    public sealed class SubstitutionCode : Code
    {
        public TeamSide Team { get; }
        /// <summary>Hráč, který odchází ze hřiště.</summary>
        public int OutPlayer { get; }
        /// <summary>Hráč, který přichází na hřiště.</summary>
        public int InPlayer { get; }

        public SubstitutionCode(
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
            (Team, OutPlayer, InPlayer) = ParseSubstitution(kod);
        }

        private static (TeamSide team, int outPlayer, int inPlayer) ParseSubstitution(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
                return (TeamSide.Home, -1, -1);

            var s = kod.Trim();
            int idx = 0;

            // team prefix
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
                // default domácí
                team = TeamSide.Home;
            }

            // typ kódu – očekáváme 'c' / 'C'
            if (idx < s.Length && (s[idx] == 'c' || s[idx] == 'C'))
                idx++;

            int outPlayer = -1;
            int inPlayer = -1;

            // očekáváme "XX:YY"
            // zbytek stringu od idx by měl být např. "04:10"
            var rest = s.Substring(idx);
            var parts = rest.Split(':');

            if (parts.Length == 2)
            {
                if (!int.TryParse(parts[0], out outPlayer))
                    outPlayer = -1;

                if (!int.TryParse(parts[1], out inPlayer))
                    inPlayer = -1;
            }

            return (team, outPlayer, inPlayer);
        }

        public override string ToString()
            => $"{GetType().Name}: Team={Team}, Out={OutPlayer}, In={InPlayer}, " +
               $"Time={RecordedAt?.ToString("HH:mm:ss") ?? "null"}, Set={SetNumber}";
    }

}
