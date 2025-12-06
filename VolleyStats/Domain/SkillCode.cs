using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VolleyStats.Enums;

namespace VolleyStats.Domain
{
    /// <summary>
    /// Základ pro všechny kódy úderů (servis, příjem, útok, blok, dig, set, free ball).
    /// Obsahuje TEAM, PLAYER NUMBER, BASIC SKILL, TYPE OF HIT, EVALUATION.
    /// </summary>
    public abstract class SkillCode : Code
    {
        public TeamSide Team { get; }
        public int PlayerNumber { get; }
        public BasicSkill Skill { get; }
        public HitType HitType { get; }
        public EvaluationSymbol Evaluation { get; }

        public char? ExtendedSymbolRaw { get; }
        public string CustomCode { get; }

        protected SkillCode(
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
            (Team, PlayerNumber, Skill, HitType, Evaluation) = ParseMainCode(kod);
            (ExtendedSymbolRaw, CustomCode) = ParseExtendedAndCustom(kod);
        }

        private static (TeamSide, int, BasicSkill, HitType, EvaluationSymbol) ParseMainCode(string kod)
        {
            var s = kod.Trim();

            TeamSide team =
                s.StartsWith("*") ? TeamSide.Home :
                s.StartsWith("a", StringComparison.OrdinalIgnoreCase) ? TeamSide.Away :
                TeamSide.Home;

            if (team == TeamSide.Home)
                s = s.TrimStart('*');
            else if (team == TeamSide.Away)
                s = s.TrimStart('a', 'A');

            int idx = 0;
            while (idx < s.Length && char.IsDigit(s[idx]))
                idx++;

            int player = -1;
            if (idx > 0 && int.TryParse(s[..idx], out var num))
                player = num;

            char? skillChar = null;
            char? typeChar = null;
            char? evalChar = null;

            if (idx < s.Length)
            {
                skillChar = s[idx];
                idx++;
            }
            if (idx < s.Length)
            {
                typeChar = s[idx];
                idx++;
            }
            if (idx < s.Length)
            {
                evalChar = s[idx];
                idx++;
            }

            var skill = skillChar switch
            {
                'S' => BasicSkill.Serve,
                'R' => BasicSkill.Reception,
                'A' => BasicSkill.Attack,
                'B' => BasicSkill.Block,
                'D' => BasicSkill.Dig,
                'E' => BasicSkill.Set,
                'F' => BasicSkill.FreeBall,
                _ => BasicSkill.Unknown
            };

            var hitType = typeChar switch
            {
                'H' => HitType.High,
                'M' => HitType.Medium,
                'Q' => HitType.Quick,
                'T' => HitType.Tense,
                'U' => HitType.Super,
                'N' => HitType.Fast,
                'O' => HitType.Other,
                _ => HitType.None
            };

            var eval = evalChar switch
            {
                '=' => EvaluationSymbol.Error,
                '/' => EvaluationSymbol.Over,
                '-' => EvaluationSymbol.Poor,
                '!' => EvaluationSymbol.Good,
                '+' => EvaluationSymbol.Positive,
                '#' => EvaluationSymbol.Point,
                _ => EvaluationSymbol.Unknown
            };

            return (team, player, skill, hitType, eval);
        }

        private static (char? ext, string custom) ParseExtendedAndCustom(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
                return (null, string.Empty);

            var core = kod.Split(';')[0].Trim();

            int idx = 0;

            if (idx < core.Length && (core[idx] == '*' || core[idx] == 'a' || core[idx] == 'A'))
                idx++;

            while (idx < core.Length && char.IsDigit(core[idx]))
                idx++;

            if (idx + 3 > core.Length)
                return (null, string.Empty);

            idx += 3;

            if (idx >= core.Length)
                return (null, string.Empty);

            var suffix = core.Substring(idx);

            string extras;
            var zoneMatch = Regex.Match(suffix, @"~~~(\d{2}[A-Za-z]?)");
            if (zoneMatch.Success)
            {
                int endOfZone = zoneMatch.Index + zoneMatch.Length;
                if (endOfZone >= suffix.Length)
                    return (null, string.Empty);

                extras = suffix.Substring(endOfZone);
            }
            else
            {
                extras = suffix;
            }

            if (string.IsNullOrEmpty(extras))
                return (null, string.Empty);

            int tildeCount = 0;
            while (tildeCount < extras.Length && extras[tildeCount] == '~')
                tildeCount++;

            var afterTildes = extras.Substring(tildeCount);
            if (afterTildes.Length == 0)
                return (null, string.Empty);

            if (afterTildes.Length == 1 && tildeCount >= 2)
            {
                return (afterTildes[0], string.Empty);
            }

            if (tildeCount >= 3 && !char.IsDigit(afterTildes[0]))
            {
                var customOnly = afterTildes;
                if (customOnly.Length > 5)
                    customOnly = customOnly[..5];
                return (null, customOnly);
            }

            char extChar = afterTildes[0];
            string custom = afterTildes[1..];
            if (custom.Length > 5)
                custom = custom[..5];

            return (extChar, custom);
        }



        public override string ToString()
            => $"{GetType().Name}: {Team} #{PlayerNumber} {Skill} {HitType} {Evaluation} ({RawCode})";
    }
}
