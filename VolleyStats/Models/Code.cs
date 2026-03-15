using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VolleyStats.Enums;

namespace VolleyStats.Models
{
    internal static class DvParsing
    {
        public static TeamSide ParseTeamFromFirstChar(string rawCode)
        {
            if (string.IsNullOrEmpty(rawCode))
                return TeamSide.Unknown;

            return rawCode[0] switch
            {
                '*' => TeamSide.Home,
                'a' => TeamSide.Away,
                _ => TeamSide.Unknown
            };
        }

        public static int ParseIntSpan(ReadOnlySpan<char> span)
        {
            if (span.Length == 0)
                throw new FormatException("Empty integer span.");

            int v = 0;
            for (int i = 0; i < span.Length; i++)
            {
                var c = span[i];
                if (c < '0' || c > '9')
                    throw new FormatException($"Non-digit '{c}' in integer span.");
                v = (v * 10) + (c - '0');
            }
            return v;
        }

        public static Skill ParseSkill(char c) => c switch
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

        public static Evaluation ParseEvaluation(char c) => c switch
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

    public class Code
    {
        public string RawLine { get; }
        public string RawCode { get; }
        public char Sp { get; }
        public char Pr { get; }

        public CourtCoordinate? Start { get; }
        public CourtCoordinate? Middle { get; }
        public CourtCoordinate? End { get; }

        public int? RecordedAt { get; }
        public int? SetNumber { get; }
        public int? HomeSetterZone { get; }
        public int? AwaySetterZone { get; }
        public int? VideoFile { get; }
        public int? VideoSecond { get; }
        public int[] HomeZones { get; } = Array.Empty<int>();
        public int[] AwayZones { get; } = Array.Empty<int>();

        public virtual CodeKind Kind => CodeKind.Unknown;
        public virtual TeamSide Team => DvParsing.ParseTeamFromFirstChar(RawCode);

        public Code(string rawLine)
        {
            RawLine = rawLine ?? string.Empty;

            var parts = SplitParts(rawLine);

            RawCode = parts.ElementAtOrDefault(0) ?? string.Empty;

            Sp = FirstCharOrDefault(parts.ElementAtOrDefault(1));
            Pr = FirstCharOrDefault(parts.ElementAtOrDefault(2));

            Start = ParseCoordinate(parts.ElementAtOrDefault(4));
            Middle = ParseCoordinate(parts.ElementAtOrDefault(5));
            End = ParseCoordinate(parts.ElementAtOrDefault(6));

            RecordedAt = ParseNullableInt(parts.ElementAtOrDefault(7));
            SetNumber = ParseNullableInt(parts.ElementAtOrDefault(8));
            HomeSetterZone = ParseNullableInt(parts.ElementAtOrDefault(9));
            AwaySetterZone = ParseNullableInt(parts.ElementAtOrDefault(10));

            VideoFile = ParseNullableInt(parts.ElementAtOrDefault(11));
            VideoSecond = ParseNullableInt(parts.ElementAtOrDefault(12));

            HomeZones = ParseZones(parts, 14);
            AwayZones = ParseZones(parts, 20);
        }

        private static string[] SplitParts(string rawLine)
        {
            return string.IsNullOrEmpty(rawLine)
                ? Array.Empty<string>()
                : rawLine.Split(';');
        }

        private static char FirstCharOrDefault(string? value)
        {
            return string.IsNullOrEmpty(value) ? '\0' : value[0];
        }

        private static CourtCoordinate? ParseCoordinate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();

            if (trimmed.Length == 4 && trimmed.All(char.IsDigit))
            {
                if (int.TryParse(trimmed.Substring(0, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)
                    && int.TryParse(trimmed.Substring(2, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
                {
                    return new CourtCoordinate(x, y);
                }
            }

            var normalized = trimmed.Replace(",", "-");
            var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2
                && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var xDash)
                && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var yDash))
            {
                return new CourtCoordinate(xDash, yDash);
            }

            return null;
        }

        private static int? ParseNullableInt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                return i;

            return null;
        }

        private static int[] ParseZones(IReadOnlyList<string> parts, int startIndex)
        {
            var zones = new List<int>(6);
            for (int i = 0; i < 6; i++)
            {
                var value = parts.ElementAtOrDefault(startIndex + i);
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var zone))
                {
                    zones.Add(zone);
                }
            }
            return zones.ToArray();
        }
    }

    public readonly record struct CourtCoordinate(int X, int Y);

    // =========================================================
    // Match-state codes
    // =========================================================

    public sealed class CodeScoreMarker : Code
    {
        public override CodeKind Kind => CodeKind.ScoreMarker;
        public override TeamSide Team { get; }

        public int? HomePoints { get; }
        public int? AwayPoints { get; }

        public CodeScoreMarker(Code baseCode, TeamSide pointWinner, int? homePoints, int? awayPoints)
            : base(baseCode.RawLine)
        {
            Team = pointWinner;
            HomePoints = homePoints;
            AwayPoints = awayPoints;
        }
    }

    public sealed class CodeLineUp : Code
    {
        public override CodeKind Kind => CodeKind.LineUp;
        public override TeamSide Team { get; }

        public int[] PlayersOnCourt { get; } = Array.Empty<int>();
        public int? SetterZone { get; }

        public CodeLineUp(Code baseCode, TeamSide team, int[] playersOnCourt, int? setterZone)
            : base(baseCode.RawLine)
        {
            Team = team;
            PlayersOnCourt = (playersOnCourt ?? Array.Empty<int>()).ToArray();
            SetterZone = setterZone;
        }
    }

    public sealed class CodeRotation : Code
    {
        public override CodeKind Kind => CodeKind.Rotation;
        public override TeamSide Team { get; }
        public int? SetterZone { get; }

        public CodeRotation(Code baseCode, TeamSide team, int? setterZone)
            : base(baseCode.RawLine)
        {
            Team = team;
            SetterZone = setterZone;
        }
    }

    public sealed class CodeTimeout : Code
    {
        public override CodeKind Kind => CodeKind.Timeout;
        public override TeamSide Team { get; }

        /// <summary>Video timestamp (seconds) when this timeout was recorded.</summary>
        public int? Time => VideoSecond;

        public CodeTimeout(Code baseCode, TeamSide team)
            : base(baseCode.RawLine)
        {
            Team = team;
        }
    }

    public sealed class CodeSubstitution : Code
    {
        public override CodeKind Kind => CodeKind.Substitution;
        public override TeamSide Team { get; }

        public int? PlayerOut { get; }
        public int? PlayerIn { get; }

        /// <summary>Video timestamp (seconds) when this substitution was recorded.</summary>
        public int? Time => VideoSecond;

        public CodeSubstitution(Code baseCode, TeamSide team, int? playerOut, int? playerIn)
            : base(baseCode.RawLine)
        {
            Team = team;
            PlayerOut = playerOut;
            PlayerIn = playerIn;
        }
    }

    public sealed class CodeEndSet : Code
    {
        public override CodeKind Kind => CodeKind.EndSet;
        public int? SetIndex { get; }

        public CodeEndSet(Code baseCode, int? setIndex)
            : base(baseCode.RawLine)
        {
            SetIndex = setIndex;
        }
    }

    public sealed class CodeGreen : Code
    {
        public override CodeKind Kind => CodeKind.Green;

        public CodeGreen(Code baseCode) : base(baseCode.RawLine) { }
    }

    // =========================================================
    // Ball-contact codes
    // =========================================================

    public abstract class BallContactCode : Code
    {
        public override CodeKind Kind => CodeKind.BallContact;

        public int? PlayerNumber { get; }
        public Skill Skill { get; }
        public char? HitType { get; }
        public Evaluation? Evaluation { get; }

        public string? Combination { get; init; }
        public char? SkillTypeExt { get; init; }
        public int? PlayersExt { get; init; }
        public char? SpecialExt { get; init; }

        protected BallContactCode(Code baseCode, int? playerNumber, Skill skill, char? hitType, Evaluation? evaluation)
            : base(baseCode.RawLine)
        {
            PlayerNumber = playerNumber;
            Skill = skill;
            HitType = hitType;
            Evaluation = evaluation;
        }
    }

    public sealed class CodeServe : BallContactCode
    {
        public CodeServe(Code baseCode, int? playerNumber, char? hitType, Evaluation? evaluation)
            : base(baseCode, playerNumber, Skill.Serve, hitType, evaluation) { }
    }

    public sealed class CodeReception : BallContactCode
    {
        public CodeReception(Code baseCode, int? playerNumber, char? hitType, Evaluation? evaluation)
            : base(baseCode, playerNumber, Skill.Reception, hitType, evaluation) { }
    }

    public sealed class CodeSet : BallContactCode
    {
        public CodeSet(Code baseCode, int? playerNumber, char? hitType, Evaluation? evaluation)
            : base(baseCode, playerNumber, Skill.Set, hitType, evaluation) { }
    }

    public sealed class CodeAttack : BallContactCode
    {
        public CodeAttack(Code baseCode, int? playerNumber, char? hitType, Evaluation? evaluation)
            : base(baseCode, playerNumber, Skill.Attack, hitType, evaluation) { }
    }

    public sealed class CodeBlock : BallContactCode
    {
        public CodeBlock(Code baseCode, int? playerNumber, char? hitType, Evaluation? evaluation)
            : base(baseCode, playerNumber, Skill.Block, hitType, evaluation) { }
    }

    public sealed class CodeDig : BallContactCode
    {
        public CodeDig(Code baseCode, int? playerNumber, char? hitType, Evaluation? evaluation)
            : base(baseCode, playerNumber, Skill.Dig, hitType, evaluation) { }
    }

    public sealed class CodeFreeBall : BallContactCode
    {
        public CodeFreeBall(Code baseCode, int? playerNumber, char? hitType, Evaluation? evaluation)
            : base(baseCode, playerNumber, Skill.FreeBall, hitType, evaluation) { }
    }

    // =========================================================
    // Factory + stream parser
    // =========================================================

    public static class CodeClassifier
    {
        public static Code ParseSingleLine(string rawLine)
        {
            var baseCode = new Code(rawLine);
            var raw = baseCode.RawCode ?? string.Empty;

            if (IsGreen(raw))
                return new CodeGreen(baseCode);

            if (IsTimeout(raw))
                return new CodeTimeout(baseCode, DvParsing.ParseTeamFromFirstChar(raw));

            if (IsEndSet(raw, out var setIndex))
                return new CodeEndSet(baseCode, setIndex);

            if (IsRotation(raw, out var rotTeam, out var rotZone))
                return new CodeRotation(baseCode, rotTeam, rotZone);

            if (IsSubstitution(raw, out var subTeam, out var outNo, out var inNo))
                return new CodeSubstitution(baseCode, subTeam, outNo, inNo);

            if (IsScoreMarker(raw, out var pointTeam, out var hp, out var ap))
                return new CodeScoreMarker(baseCode, pointTeam, hp, ap);

            if (IsBallContact(raw, out var team, out var player, out var skill, out var hitType, out var eval))
            {
                return skill switch
                {
                    Skill.Serve      => new CodeServe(baseCode, player, hitType, eval),
                    Skill.Reception  => new CodeReception(baseCode, player, hitType, eval),
                    Skill.Set        => new CodeSet(baseCode, player, hitType, eval),
                    Skill.Attack     => new CodeAttack(baseCode, player, hitType, eval),
                    Skill.Block      => new CodeBlock(baseCode, player, hitType, eval),
                    Skill.Dig        => new CodeDig(baseCode, player, hitType, eval),
                    Skill.FreeBall   => new CodeFreeBall(baseCode, player, hitType, eval),
                    _                => baseCode
                };
            }

            return baseCode;
        }

        public static IEnumerable<Code> ParseLines(IEnumerable<string> rawLines)
        {
            PendingLineUp? pendingHome = null;
            PendingLineUp? pendingAway = null;

            foreach (var line in rawLines)
            {
                var baseCode = new Code(line);
                var raw = baseCode.RawCode ?? string.Empty;

                if (IsLineUpPlayers(raw, out var teamPlayers))
                {
                    var players = ExtractPlayersOnCourt(baseCode, teamPlayers);
                    var pending = new PendingLineUp(baseCode, teamPlayers, players);

                    if (teamPlayers == TeamSide.Home) pendingHome = pending;
                    else if (teamPlayers == TeamSide.Away) pendingAway = pending;
                    continue;
                }

                if (IsLineUpSetterZone(raw, out var teamZone, out var setterZone))
                {
                    var p = teamZone == TeamSide.Home ? pendingHome : pendingAway;
                    if (p != null)
                    {
                        yield return new CodeLineUp(p.BaseCode, teamZone, p.PlayersOnCourt, setterZone);
                        if (teamZone == TeamSide.Home) pendingHome = null;
                        else pendingAway = null;
                        continue;
                    }

                    yield return baseCode;
                    continue;
                }

                yield return ParseSingleLine(line);
            }

            if (pendingHome != null) yield return pendingHome.BaseCode;
            if (pendingAway != null) yield return pendingAway.BaseCode;
        }

        private sealed record PendingLineUp(Code BaseCode, TeamSide Team, int[] PlayersOnCourt);

        private static bool IsGreen(string raw) =>
            raw.StartsWith("*$$", StringComparison.Ordinal) || raw.StartsWith("a$$", StringComparison.Ordinal);

        private static bool IsTimeout(string raw) => raw == "*T" || raw == "aT";

        private static bool IsEndSet(string raw, out int? setIndex)
        {
            setIndex = null;
            if (raw.Length >= 6 && raw.StartsWith("**", StringComparison.Ordinal) &&
                raw.EndsWith("set", StringComparison.OrdinalIgnoreCase))
            {
                if (raw.Length >= 3 && char.IsDigit(raw[2]))
                {
                    setIndex = raw[2] - '0';
                    return true;
                }
            }
            return false;
        }

        private static bool IsRotation(string raw, out TeamSide team, out int? setterZone)
        {
            team = TeamSide.Unknown;
            setterZone = null;

            if (raw.Length >= 3 && (raw[0] == '*' || raw[0] == 'a') && raw[1] == 'z' && char.IsDigit(raw[2]) &&
                !raw.Contains(">LUp", StringComparison.OrdinalIgnoreCase))
            {
                team = raw[0] == '*' ? TeamSide.Home : TeamSide.Away;
                setterZone = raw[2] - '0';
                return true;
            }
            return false;
        }

        private static bool IsSubstitution(string raw, out TeamSide team, out int? playerOut, out int? playerIn)
        {
            team = TeamSide.Unknown;
            playerOut = null;
            playerIn = null;

            if (raw.Length >= 6 && (raw[0] == '*' || raw[0] == 'a') && raw[1] == 'c')
            {
                team = raw[0] == '*' ? TeamSide.Home : TeamSide.Away;
                var span = raw.AsSpan(2);

                var colon = span.IndexOf(':');
                if (colon <= 0) return false;

                var left = span.Slice(0, colon);
                var right = span.Slice(colon + 1);

                int len = 0;
                while (len < right.Length && char.IsDigit(right[len])) len++;
                if (len == 0) return false;

                playerOut = DvParsing.ParseIntSpan(left);
                playerIn = DvParsing.ParseIntSpan(right.Slice(0, len));
                return true;
            }
            return false;
        }

        private static bool IsScoreMarker(string raw, out TeamSide pointWinner, out int? homePoints, out int? awayPoints)
        {
            pointWinner = TeamSide.Unknown;
            homePoints = null;
            awayPoints = null;

            if (raw.Length >= 6 && (raw.StartsWith("*p", StringComparison.Ordinal) || raw.StartsWith("ap", StringComparison.Ordinal)))
            {
                pointWinner = raw[0] == '*' ? TeamSide.Home : TeamSide.Away;

                var span = raw.AsSpan(2);
                var colon = span.IndexOf(':');
                if (colon <= 0) return false;

                var left = span.Slice(0, colon);
                var right = span.Slice(colon + 1);

                int len = 0;
                while (len < right.Length && char.IsDigit(right[len])) len++;
                if (len == 0) return false;

                homePoints = DvParsing.ParseIntSpan(left);
                awayPoints = DvParsing.ParseIntSpan(right.Slice(0, len));
                return true;
            }
            return false;
        }

        private static bool IsLineUpPlayers(string raw, out TeamSide team)
        {
            team = TeamSide.Unknown;

            if (raw.Length >= 7 &&
                (raw[0] == '*' || raw[0] == 'a') &&
                raw[1] == 'P' &&
                char.IsDigit(raw[2]) && char.IsDigit(raw[3]) &&
                raw.EndsWith(">LUp", StringComparison.OrdinalIgnoreCase))
            {
                team = raw[0] == '*' ? TeamSide.Home : TeamSide.Away;
                return true;
            }
            return false;
        }

        private static bool IsLineUpSetterZone(string raw, out TeamSide team, out int? setterZone)
        {
            team = TeamSide.Unknown;
            setterZone = null;

            if (raw.Length >= 6 &&
                (raw[0] == '*' || raw[0] == 'a') &&
                raw[1] == 'z' &&
                char.IsDigit(raw[2]) &&
                raw.EndsWith(">LUp", StringComparison.OrdinalIgnoreCase))
            {
                team = raw[0] == '*' ? TeamSide.Home : TeamSide.Away;
                setterZone = raw[2] - '0';
                return true;
            }
            return false;
        }

        private static int[] ExtractPlayersOnCourt(Code baseCode, TeamSide team)
        {
            return team == TeamSide.Home
                ? (baseCode.HomeZones ?? Array.Empty<int>()).ToArray()
                : (baseCode.AwayZones ?? Array.Empty<int>()).ToArray();
        }

        private static bool IsBallContact(string raw,
            out TeamSide team,
            out int? playerNumber,
            out Skill skill,
            out char? hitType,
            out Evaluation? evaluation)
        {
            team = TeamSide.Unknown;
            playerNumber = null;
            skill = Skill.Unknown;
            hitType = null;
            evaluation = null;

            if (raw.Length < 6) return false;
            if (raw[0] != '*' && raw[0] != 'a') return false;
            if (!char.IsDigit(raw[1]) || !char.IsDigit(raw[2])) return false;

            team = raw[0] == '*' ? TeamSide.Home : TeamSide.Away;
            playerNumber = (raw[1] - '0') * 10 + (raw[2] - '0');

            skill = DvParsing.ParseSkill(raw[3]);
            if (skill == Skill.Unknown) return false;

            hitType = raw[4];
            evaluation = DvParsing.ParseEvaluation(raw[5]);
            return true;
        }
    }
}
