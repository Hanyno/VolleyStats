using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VolleyStats.Domain
{
    public sealed class ServeCode : SkillCode
    {
        public int? StartZone { get; }
        public int? EndZone { get; }
        public char? EndSubZone { get; }

        public ServeCode(
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
            string videoFile,
            int? videoSecond,
            int[] homeZones,
            int[] awayZones)
            : base(rawLine, kod, sp, pr,
                   start, middle, end,
                   recordedAt, setNumber,
                   homeSetterZone, awaySetterZone,
                   videoFile, videoSecond,
                   homeZones, awayZones)
        {
            (StartZone, EndZone, EndSubZone) = ParseServeZones(kod);
        }

        private static (int? start, int? end, char? sub) ParseServeZones(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
                return (null, null, null);

            var mZone = Regex.Match(kod, @"(\d{2})([A-Za-z])?$");
            if (!mZone.Success)
                return (null, null, null);

            var pair = mZone.Groups[1].Value;
            int? sz = null;
            int? ez = null;
            if (pair.Length == 2 &&
                int.TryParse(pair[0].ToString(), out var s) &&
                int.TryParse(pair[1].ToString(), out var e))
            {
                sz = s;
                ez = e;
            }

            char? sub = null;
            if (mZone.Groups[2].Success)
                sub = mZone.Groups[2].Value[0];

            return (sz, ez, sub);
        }

        public override string ToString()
            => base.ToString() + $" Zones {StartZone}->{EndZone}{EndSubZone}";
    }

}
