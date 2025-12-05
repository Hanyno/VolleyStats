using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VolleyStats.Enums;

namespace VolleyStats.Domain
{
    public sealed class GreenCode : Code
    {
        public TeamSide Team { get; }
        public char Variant { get; }   // '#', '=', případně jiný

        public GreenCode(
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
            (Team, Variant) = Parse(kod);
        }

        private static (TeamSide team, char variant) Parse(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
                return (TeamSide.Home, '#');

            var s = kod.Trim();
            int idx = 0;

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
                team = TeamSide.Home;
            }

            // poslední znak jako varianta (#/=/cokoliv)
            var variant = s[^1];

            return (team, variant);
        }

        public override string ToString()
            => $"{GetType().Name}: Team={Team}, Variant={Variant}, " +
               $"Set={SetNumber}, Time={RecordedAt?.ToString("HH:mm:ss") ?? "null"}";
    }

}
