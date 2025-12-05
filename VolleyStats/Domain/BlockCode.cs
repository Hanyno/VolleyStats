using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolleyStats.Domain
{
    public sealed class BlockCode : SkillCode
    {
        /// <summary>
        /// Koncová zóna bloku (1–6), nebo null, pokud není uvedena.
        /// </summary>
        public int? EndZone { get; }

        /// <summary>
        /// Subzóna (A/B/...), nebo null, pokud není uvedena.
        /// </summary>
        public char? EndSubZone { get; }

        public BlockCode(
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
            (EndZone, EndSubZone) = ParseBlockZones(kod);
        }

        private static (int? endZone, char? subZone) ParseBlockZones(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
                return (null, null);

            var core = kod.Split(';')[0].Trim();

            int idx = 0;

            // týmový prefix (* / a / A)
            if (idx < core.Length && (core[idx] == '*' || core[idx] == 'a' || core[idx] == 'A'))
                idx++;

            // číslo hráče
            while (idx < core.Length && char.IsDigit(core[idx]))
                idx++;

            // skill(B), hit type, evaluation → 3 znaky
            if (idx + 3 > core.Length)
                return (null, null);

            idx += 3;

            if (idx >= core.Length)
                return (null, null);

            // suffix typu "~~~~2" nebo "~~~~2A"
            var suffix = core.Substring(idx);

            // od konce: poslední znak má být zóna (číslice)
            suffix = suffix.Trim();
            if (suffix.Length == 0)
                return (null, null);

            char last = suffix[^1];
            if (!char.IsDigit(last))
                return (null, null);

            int endZone = last - '0';

            // subzóna je případně znak před zónou, pokud je to písmeno
            char? subZone = null;
            if (suffix.Length >= 2)
            {
                char prev = suffix[^2];
                if (char.IsLetter(prev))
                    subZone = prev;
            }

            return (endZone, subZone);
        }

        public override string ToString()
            => base.ToString()
               + $" BlockEnd Zone={EndZone}{EndSubZone}";
    }

}
