using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                '/' => EvaluationSymbol.VeryPositive,
                '-' => EvaluationSymbol.Poor,
                '!' => EvaluationSymbol.Good,
                '+' => EvaluationSymbol.Positive,
                '#' => EvaluationSymbol.Point,
                _ => EvaluationSymbol.Unknown
            };

            return (team, player, skill, hitType, eval);
        }

        public override string ToString()
            => $"{GetType().Name}: {Team} #{PlayerNumber} {Skill} {HitType} {Evaluation} ({Kod})";
    }
}
