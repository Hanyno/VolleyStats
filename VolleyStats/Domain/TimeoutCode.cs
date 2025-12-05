using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VolleyStats.Enums;

namespace VolleyStats.Domain
{
    /// <summary>
    /// Timeout – kód je "*T" pro domácí, "aT"/"AT" pro hosty.
    /// </summary>
    public sealed class TimeoutCode : Code
    {
        public TeamSide Team { get; }

        public TimeoutCode(
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
            Team = ParseTeamFromTimeoutCode(kod);
        }

        private static TeamSide ParseTeamFromTimeoutCode(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
                return TeamSide.Home; // fallback

            var s = kod.Trim();

            if (s.StartsWith("*"))
                return TeamSide.Home;

            if (s.StartsWith("a", StringComparison.OrdinalIgnoreCase))
                return TeamSide.Away;

            // kdyby se v budoucnu objevila nějaká varianta bez prefixu,
            // klidně necháme default Home
            return TeamSide.Home;
        }

        public override string ToString()
            => $"{GetType().Name}: Team={Team}, Code={RawCode}, Time={RecordedAt?.ToString("HH:mm:ss") ?? "null"}, Set={SetNumber}";
    }
}
