using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolleyStats.Domain
{
    /// <summary>
    /// Kód konce setu, např. "**3set".
    /// Číslo v kódu určuje číslo setu.
    /// </summary>
    public sealed class EndOfSetCode : Code
    {
        /// <summary>
        /// Číslo setu podle kódu (např. 3 z "**3set").
        /// Může se lišit od SetNumber z metadat, pokud by DV něco posunul.
        /// </summary>
        public int SetIndexFromCode { get; }

        public EndOfSetCode(
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
            SetIndexFromCode = ParseSetIndex(kod);
        }

        private static int ParseSetIndex(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
                return 0;

            var s = kod.Trim();

            // očekáváme něco jako "**3set", "*3set", "3set"...
            var idxSet = s.IndexOf("set", StringComparison.OrdinalIgnoreCase);
            if (idxSet <= 0)
                return 0;

            // jdeme zpět před "set" a hledáme čísla
            int endDigit = idxSet - 1;
            while (endDigit >= 0 && char.IsWhiteSpace(s[endDigit]))
                endDigit--;

            if (endDigit < 0)
                return 0;

            int startDigit = endDigit;
            while (startDigit >= 0 && char.IsDigit(s[startDigit]))
                startDigit--;

            startDigit++;

            if (startDigit > endDigit)
                return 0;

            var numberSpan = s.Substring(startDigit, endDigit - startDigit + 1);

            return int.TryParse(numberSpan, out var n) ? n : 0;
        }

        public override string ToString()
            => $"{GetType().Name}: End of set {SetIndexFromCode} (Set meta={SetNumber}), " +
               $"Time={RecordedAt?.ToString("HH:mm:ss") ?? "null"}";
    }
}
