using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VolleyStats.Enums;

namespace VolleyStats.Domain
{
    public sealed class ReceptionCode : SkillCode
    {
        public int? StartZone { get; }
        public int? EndZone { get; }
        public char? EndSubZone { get; }

        public ReceptionDirection Side { get; }
        public ReceptionPlayers Players { get; }
        public ReceptionErrorReason ErrorReason { get; }

        public string CustomReceptionCode { get; }

        public ReceptionCode(
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
                   videoFile, videoSecond,
                   homeZones, awayZones)
        {
            (StartZone, EndZone, EndSubZone) = ParseReceptionZones(kod);
            (Side, Players, ErrorReason, CustomReceptionCode) =
                ParseReceptionExtendedAndCustom(kod, Evaluation);
        }


        private static (int? start, int? end, char? sub) ParseReceptionZones(string kod)
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

            var mZone = Regex.Match(suffix, @"~~~(\d{2})([A-Za-z~]?)");
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
            {
                var c = mZone.Groups[2].Value[0];
                sub = c == '~' ? (char?)null : c;
            }

            return (sz, ez, sub);
        }


        private static (
            ReceptionDirection side,
            ReceptionPlayers players,
            ReceptionErrorReason errorReason,
            string custom) ParseReceptionExtendedAndCustom(string kod, EvaluationSymbol eval)
        {
            if (string.IsNullOrWhiteSpace(kod))
                return (ReceptionDirection.None, ReceptionPlayers.None, ReceptionErrorReason.None, string.Empty);

            var core = kod.Split(';')[0].Trim();

            int idx = 0;

            if (idx < core.Length && (core[idx] == '*' || core[idx] == 'a' || core[idx] == 'A'))
                idx++;

            while (idx < core.Length && char.IsDigit(core[idx]))
                idx++;

            if (idx + 3 > core.Length)
                return (ReceptionDirection.None, ReceptionPlayers.None, ReceptionErrorReason.None, string.Empty);

            idx += 3;

            if (idx >= core.Length)
                return (ReceptionDirection.None, ReceptionPlayers.None, ReceptionErrorReason.None, string.Empty);

            var suffix = core.Substring(idx);

            string extras;
            var mZone = Regex.Match(suffix, @"~~~(\d{2}[A-Za-z~]?)");
            if (mZone.Success)
            {
                int endOfZone = mZone.Index + mZone.Length;
                if (endOfZone >= suffix.Length)
                    return (ReceptionDirection.None, ReceptionPlayers.None, ReceptionErrorReason.None, string.Empty);

                extras = suffix.Substring(endOfZone);
            }
            else
            {
                extras = suffix;
            }

            if (string.IsNullOrEmpty(extras))
                return (ReceptionDirection.None, ReceptionPlayers.None, ReceptionErrorReason.None, string.Empty);

            char e1 = '~', e2 = '~', e3 = '~';

            if (extras.Length >= 1) e1 = extras[0];
            if (extras.Length >= 2) e2 = extras[1];
            if (extras.Length >= 3) e3 = extras[2];

            string custom = extras.Length > 3 ? extras.Substring(3) : string.Empty;
            if (custom.Length > 5)
                custom = custom[..5];

            var side = MapReceptionSide(e1);
            var players = MapReceptionPlayers(e2);
            var errorReason = MapReceptionErrorReason(e3, eval);

            return (side, players, errorReason, custom);
        }

        private static ReceptionDirection MapReceptionSide(char c)
        {
            return c switch
            {
                'L' => ReceptionDirection.Left,
                'R' => ReceptionDirection.Right,
                'W' => ReceptionDirection.Low,
                'O' => ReceptionDirection.Overhead,
                'M' => ReceptionDirection.Middleline,
                '~' or '\0' => ReceptionDirection.None,
                _ => ReceptionDirection.None
            };
        }

        private static ReceptionPlayers MapReceptionPlayers(char c)
        {
            return c switch
            {
                '1' => ReceptionPlayers.II_PlayersLeft,
                '2' => ReceptionPlayers.II_PlayersRight,
                '3' => ReceptionPlayers.III_PlayersLeft,
                '4' => ReceptionPlayers.III_PlayersCenter,
                '5' => ReceptionPlayers.III_PlayersRight,
                '6' => ReceptionPlayers.IV_PlayersLeft,
                '7' => ReceptionPlayers.IV_PlayersLeftCenter,
                '8' => ReceptionPlayers.IV_PlayersRightCenter,
                '9' => ReceptionPlayers.IV_PlayersRight,
                '~' or '\0' => ReceptionPlayers.None,
                _ => ReceptionPlayers.None
            };
        }

        private static ReceptionErrorReason MapReceptionErrorReason(char c, EvaluationSymbol eval)
        {
            if (eval != EvaluationSymbol.Error)
                return ReceptionErrorReason.None;

            return c switch
            {
                'U' => ReceptionErrorReason.Unplayable,
                'X' => ReceptionErrorReason.BodyError,
                'P' => ReceptionErrorReason.PositionError,
                'E' => ReceptionErrorReason.LackOfEffort,
                'Z' => ReceptionErrorReason.RefereeCall,
                '~' or '\0' => ReceptionErrorReason.None,
                _ => ReceptionErrorReason.None
            };
        }

        public override string ToString()
            => base.ToString()
               + $" Zones {StartZone}->{EndZone}{EndSubZone}, " +
                  $"Ext=({Side}, {Players}, {ErrorReason}), Custom={CustomReceptionCode}";
    }
}
