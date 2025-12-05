using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VolleyStats.Enums;

namespace VolleyStats.Domain
{
    public sealed class SetCode : SkillCode
    {
        /// <summary>
        /// Dvoupísmenný kód setter callu (např. "K7", "KM") nebo null, pokud je "~~".
        /// </summary>
        public string? SetterCallCode { get; }

        /// <summary>
        /// B/F/... – enum, který už máš vytvořený.
        /// </summary>
        public SetEnd SetEnd { get; }

        /// <summary>
        /// Koncová zóna nahrávky (2, 3, 4...), nebo null, pokud je '~'.
        /// </summary>
        public int? EndZone { get; }

        /// <summary>
        /// Subzóna (A, B, ...), nebo null, pokud je '~'.
        /// </summary>
        public char? EndSubZone { get; }

        /// <summary>
        /// Extended info pro set (tvůj enum), např. '2' → něco.
        /// </summary>
        public SetExtendedInfo SetExtendedInfo { get; }
        public SetExtendedError SetExtendedError { get; }

        /// <summary>
        /// Custom kód na konci (0–5 znaků).
        /// </summary>
        public string CustomSetCode { get; }

        public SetCode(
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
            (SetterCallCode,
             SetEnd,
             EndZone,
             EndSubZone,
             SetExtendedInfo,
             SetExtendedError,
             CustomSetCode) = ParseSetExtendedAndCustom(kod);
        }

        /// <summary>
        /// Parsuje část za základním kódem (po a09ET+, *13ET! atd.).
        /// Formát suffixu:
        ///
        /// 0-1: setterCall (např. "K7", "KM" nebo "~~" = žádný)
        /// 2  : SetEnd (B/F nebo '~')
        /// 3  : placeholder '~'
        /// 4  : endZone (číslice nebo '~')
        /// 5  : subzone (písmeno nebo '~')
        /// 6  : SetExtendedInfo (např. '2' nebo '~')
        /// 7  : SetExtendedError (např. 'Z' nebo '~')
        /// 8+ : custom code (0-5 znaků, může začínat i '~')
        /// </summary>
        private static (
            string? setterCall,
            SetEnd setEnd,
            int? endZone,
            char? subZone,
            SetExtendedInfo extendedInfo,
            SetExtendedError extendedError,
            string custom) ParseSetExtendedAndCustom(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
                return (null, SetEnd.None, null, null, SetExtendedInfo.None, SetExtendedError.None,string.Empty);

            // Stejný pattern jako v SkillCode/ServeCode/ReceptionCode:
            var core = kod.Split(';')[0].Trim();

            int idx = 0;

            // týmový prefix (* / a / A)
            if (idx < core.Length && (core[idx] == '*' || core[idx] == 'a' || core[idx] == 'A'))
                idx++;

            // číslo hráče
            while (idx < core.Length && char.IsDigit(core[idx]))
                idx++;

            // skill (E), hit type (H/T/Q...), evaluation (=, +, !, /, -)
            if (idx + 3 > core.Length)
                return (null, SetEnd.None, null, null, SetExtendedInfo.None, SetExtendedError.None, string.Empty);

            idx += 3;

            if (idx >= core.Length)
                return (null, SetEnd.None, null, null, SetExtendedInfo.None, SetExtendedError.None, string.Empty);

            var suffix = core.Substring(idx);

            // --- 0-1: setterCall ---
            string? setterCall = null;
            if (suffix.Length >= 2)
            {
                var sc = suffix.Substring(0, 2);
                if (sc != "~~")
                    setterCall = sc;
            }

            // --- 2: SetEnd ---
            char setEndChar = suffix.Length >= 3 ? suffix[2] : '~';
            var setEnd = MapSetEnd(setEndChar);

            // --- 4: endZone ---
            int? endZone = null;
            if (suffix.Length >= 5)
            {
                var c = suffix[4];
                if (char.IsDigit(c))
                    endZone = c - '0';
            }

            // --- 5: subZone ---
            char? subZone = null;
            if (suffix.Length >= 6)
            {
                var c = suffix[5];
                if (c != '~')
                    subZone = c;
            }

            // --- 6: SetExtendedInfo ---
            char extChar = suffix.Length >= 7 ? suffix[6] : '~';
            var extendedInfo = MapSetExtendedInfo(extChar);
            char exteChar = suffix.Length >= 8 ? suffix[7] : '~';
            var extendedError = MapSetExtendedError(exteChar);

            // --- 8+: custom code (max 5 znaků) ---
            string custom = string.Empty;
            if (suffix.Length > 8)
            {
                custom = suffix.Substring(8);
                if (custom.Length > 5)
                    custom = custom[..5];
            }

            return (setterCall, setEnd, endZone, subZone, extendedInfo, extendedError, custom);
        }

        private static SetEnd MapSetEnd(char c)
        {
            // Placeholder implementace – doplň podle svého enumu SetEnd.
            // Např.:
            //  'B' => SetEnd.Back,
            //  'F' => SetEnd.Front,
            //  '~' nebo '\0' => SetEnd.None

            return c switch
            {
                '~' or '\0' => SetEnd.None,
                _ => SetEnd.None   // TODO: doplnit mapování
            };
        }

        private static SetExtendedInfo MapSetExtendedInfo(char c)
        {
            // Placeholder implementace – doplň podle svého enumu SetExtendedInfo.
            // Např. číslice '1','2','3' mapovat na konkrétní hodnoty.
            return c switch
            {
                '~' or '\0' => SetExtendedInfo.None,
                _ => SetExtendedInfo.None   // TODO: doplnit mapování
            };
        }

        private static SetExtendedError MapSetExtendedError(char c)
        {
            // Placeholder implementace – doplň podle svého enumu SetExtendedInfo.
            // Např. číslice '1','2','3' mapovat na konkrétní hodnoty.
            return c switch
            {
                '~' or '\0' => SetExtendedError.None,
                _ => SetExtendedError.None   // TODO: doplnit mapování
            };
        }

        public override string ToString()
            => base.ToString()
               + $" SetCall={SetterCallCode}, End={SetEnd}, Zone={EndZone}{EndSubZone}, Ext={SetExtendedInfo}, Custom={CustomSetCode}";
    }

}
