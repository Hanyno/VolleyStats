using System.Globalization;
using VolleyStats.Enums;

namespace VolleyStats.Models
{
    /// <summary>
    /// Rozšiřuje <see cref="Code"/> o parsování hlavního kódu (team, číslo hráče, skill, typ úderu, evaluaci).
    /// </summary>
    public abstract class SkillCode : Code
    {
        public TeamSide Team { get; }
        public int PlayerNumber { get; }
        public Skill Skill { get; }
        public HitType TypeOfHit { get; }
        public Evaluation Evaluation { get; }

        protected SkillCode(string rawLine) : base(rawLine)
        {
            var main = RawCode ?? string.Empty;
            var index = 0;

            Team = ParseTeam(main, ref index);
            PlayerNumber = ParsePlayerNumber(main, ref index);
            Skill = ParseSkill(main, ref index);
            TypeOfHit = ParseHitType(main, ref index);
            Evaluation = ParseEvaluation(main, ref index);
        }

        internal static char PeekSkillLetter(string rawLine)
        {
            var main = (rawLine ?? string.Empty).Split(';')[0];
            var index = 0;
            ParseTeam(main, ref index);
            ParsePlayerNumber(main, ref index);
            return index < main.Length ? char.ToUpperInvariant(main[index]) : '\0';
        }

        private static TeamSide ParseTeam(string main, ref int index)
        {
            if (index < main.Length)
            {
                var c = main[index];
                if (c == 'a' || c == 'A')
                {
                    index++;
                    return TeamSide.Away;
                }

                if (c == '*')
                {
                    index++;
                    return TeamSide.Home;
                }
            }

            return TeamSide.Home;
        }

        private static int ParsePlayerNumber(string main, ref int index)
        {
            var start = index;
            while (index < main.Length && index - start < 2 && char.IsDigit(main[index]))
            {
                index++;
            }

            var spanLength = index - start;
            if (spanLength == 0)
                return 0;

            var numberSpan = main.Substring(start, spanLength);
            if (int.TryParse(numberSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var num))
                return num;

            return 0;
        }

        private static Skill ParseSkill(string main, ref int index)
        {
            if (index >= main.Length)
                return Skill.Unknown;

            var c = char.ToUpperInvariant(main[index]);
            index++;
            return c switch
            {
                'S' => Skill.Serve,
                'R' => Skill.Reception,
                'A' => Skill.Attack,
                'B' => Skill.Block,
                'D' => Skill.Dig,
                'E' => Skill.Set,
                'F' => Skill.FreeBall,
                _ => Skill.Unknown
            };
        }

        private static HitType ParseHitType(string main, ref int index)
        {
            if (index >= main.Length)
                return HitType.Unknown;

            var c = char.ToUpperInvariant(main[index]);
            index++;
            return c switch
            {
                'H' => HitType.High,
                'M' => HitType.Medium,
                'Q' => HitType.Quick,
                'T' => HitType.Tense,
                'U' => HitType.Super,
                'N' => HitType.Fast,
                'O' => HitType.Other,
                _ => HitType.Unknown
            };
        }

        private static Evaluation ParseEvaluation(string main, ref int index)
        {
            if (index >= main.Length)
                return Evaluation.Unknown;

            var c = main[index];
            index++;
            return c switch
            {
                '=' => Evaluation.Error,
                '/' => Evaluation.VeryPoorOrBlocked,
                '-' => Evaluation.Poor,
                '!' => Evaluation.InsufficientOrCovered,
                '+' => Evaluation.Positive,
                '#' => Evaluation.Point,
                _ => Evaluation.Unknown
            };
        }
    }

    public sealed class ServeCode : SkillCode
    {
        public ServeCode(string rawLine) : base(rawLine) { }
    }

    public sealed class ReceptionCode : SkillCode
    {
        public ReceptionCode(string rawLine) : base(rawLine) { }
    }

    public sealed class AttackCode : SkillCode
    {
        public AttackCode(string rawLine) : base(rawLine) { }
    }

    public sealed class BlockCode : SkillCode
    {
        public BlockCode(string rawLine) : base(rawLine) { }
    }

    public sealed class DigCode : SkillCode
    {
        public DigCode(string rawLine) : base(rawLine) { }
    }

    public sealed class SetCode : SkillCode
    {
        public SetCode(string rawLine) : base(rawLine) { }
    }

    public sealed class FreeBallCode : SkillCode
    {
        public FreeBallCode(string rawLine) : base(rawLine) { }
    }

    public static class SkillCodeFactory
    {
        public static SkillCode? Create(string rawLine)
        {
            var skillLetter = SkillCode.PeekSkillLetter(rawLine);
            return skillLetter switch
            {
                'S' => new ServeCode(rawLine),
                'R' => new ReceptionCode(rawLine),
                'A' => new AttackCode(rawLine),
                'B' => new BlockCode(rawLine),
                'D' => new DigCode(rawLine),
                'E' => new SetCode(rawLine),
                'F' => new FreeBallCode(rawLine),
                _ => null
            };
        }
    }
}
