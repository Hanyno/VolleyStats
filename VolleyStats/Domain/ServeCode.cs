using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VolleyStats.Enums;

namespace VolleyStats.Domain
{
    public sealed class ServeCode : SkillCode
    {
        public int? StartZone { get; }
        public int? EndZone { get; }
        public char? EndSubZone { get; }

        public ServeExtendedInfo ExtendedInfo { get; }

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
            ExtendedInfo = MapExtendedInfoForServe(ExtendedSymbolRaw);
        }

        private static (int? start, int? end, char? sub) ParseServeZones(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
                return (null, null, null);

            var core = kod.Split(';')[0].Trim();

            int idx = 0;
            if (idx < core.Length && (core[idx] == '*' || core[idx] == 'a' || core[idx] == 'A'))
                idx++;

            while (idx < core.Length && char.IsDigit(core[idx]))
                idx++;

            if (idx + 3 > core.Length)
                return (null, null, null);

            idx += 3;

            if (idx >= core.Length)
                return (null, null, null);

            var suffix = core.Substring(idx);

            var mZone = Regex.Match(suffix, @"~~~(\d{2})([A-Za-z]?)");
            if (!mZone.Success)
                return (null, null, null);

            var pair = mZone.Groups[1].Value;
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

            if (mZone.Groups[2].Success && mZone.Groups[2].Value.Length == 1)
                sub = mZone.Groups[2].Value[0];

            return (sz, ez, sub);
        }


        private static ServeExtendedInfo MapExtendedInfoForServe(char? ext)
        {
            return ext switch
            {
                'N' => ServeExtendedInfo.Net,
                'O' => ServeExtendedInfo.OutLong,
                'R' => ServeExtendedInfo.OutRight,
                'L' => ServeExtendedInfo.OutLeft,
                _ => ServeExtendedInfo.None
            };
        }

        public override string ToString()
            => base.ToString() + $" Zones {StartZone}->{EndZone}{EndSubZone}, Ext={ExtendedSymbolRaw}/{ExtendedInfo}, Custom={CustomCode}";
    }

}
