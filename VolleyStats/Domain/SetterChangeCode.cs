using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VolleyStats.Enums;

namespace VolleyStats.Domain
{
    /// <summary>
    /// Změna nahrávače – kód typu "*P9" / "aP13".
    /// Označuje, který hráč je aktuálně nahrávač.
    /// </summary>
    public sealed class SetterChangeCode : Code
    {
        /// <summary>
        /// Tým, kterého se změna týká (Home/Away).
        /// </summary>
        public TeamSide Team { get; }

        /// <summary>
        /// Číslo hráče, který je po této změně nahrávač.
        /// </summary>
        public int SetterNumber { get; }

        public SetterChangeCode(
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
            (Team, SetterNumber) = ParseSetterChange(kod);
        }

        private static (TeamSide team, int setterNum) ParseSetterChange(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
                return (TeamSide.Home, -1);

            var s = kod.Trim();
            int idx = 0;

            // prefix týmu
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
                // fallback – když by někdy prefix chyběl
                team = TeamSide.Home;
            }

            // očekáváme 'P' / 'p'
            if (idx < s.Length && (s[idx] == 'P' || s[idx] == 'p'))
                idx++;

            int setterNum = -1;

            var numPart = s.Substring(idx);
            if (!int.TryParse(numPart, out setterNum))
                setterNum = -1;

            return (team, setterNum);
        }

        public override string ToString()
            => $"{GetType().Name}: Team={Team}, Setter={SetterNumber}, " +
               $"Time={RecordedAt?.ToString("HH:mm:ss") ?? "null"}, Set={SetNumber}";
    }
}
