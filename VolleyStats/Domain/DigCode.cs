using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VolleyStats.Domain
{
    public sealed class DigCode : SkillCode
    {
        public int? StartZone { get; }
        public int? EndZone { get; }
        public char? EndSubZone { get; }

        public DigCode(
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
            : base(rawLine, kod, sp, pr,
                   start, middle, end,
                   recordedAt, setNumber,
                   homeSetterZone, awaySetterZone,
                   videoFile ?? string.Empty,
                   videoSecond,
                   homeZones ?? Array.Empty<int>(),
                   awayZones ?? Array.Empty<int>())
        {
            (StartZone, EndZone, EndSubZone) = ParseDigZones(kod);
        }

        private static (int? start, int? end, char? sub) ParseDigZones(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
                return (null, null, null);

            var core = kod.Split(';')[0].Trim();

            int idx = 0;

            // týmový prefix (* / a / A)
            if (idx < core.Length && (core[idx] == '*' || core[idx] == 'a' || core[idx] == 'A'))
                idx++;

            // číslo hráče
            while (idx < core.Length && char.IsDigit(core[idx]))
                idx++;

            // skill (D), hit type, evaluation → 3 znaky
            if (idx + 3 > core.Length)
                return (null, null, null);

            idx += 3;

            if (idx >= core.Length)
                return (null, null, null);

            var suffix = core.Substring(idx);

            // ~~~27A -> group1 = "27", group2 = "A" (nebo "~"/prázdné)
            var mZone = Regex.Match(suffix, @"~~~(\d{2})([A-Za-z~]?)");
            if (!mZone.Success)
                return (null, null, null);

            var pair = mZone.Groups[1].Value;
            var subRaw = mZone.Groups[2].Value;

            int? sz = null;
            int? ez = null;
            char? sub = null;

            if (pair.Length == 2 &&
                int.TryParse(pair[0].ToString(), out var s) &&
                int.TryParse(pair[1].ToString(), out var e))
            {
                sz = s;
                ez = e;
            }

            if (!string.IsNullOrEmpty(subRaw) && subRaw != "~")
                sub = subRaw[0];

            return (sz, ez, sub);
        }

        public override string ToString()
            => base.ToString()
               + $" Dig Zones {StartZone}->{EndZone}{EndSubZone}";
    }

}
