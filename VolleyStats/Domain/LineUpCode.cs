using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VolleyStats.Enums;

namespace VolleyStats.Domain
{
    public sealed class LineUpCode : Code
    {
        public TeamSide Team { get; }

        public int? SetterPlayerNumber { get; }

        public int? SetterZone { get; }

        public bool IsLineUp => true;

        public LineUpCode(
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
            (Team, SetterPlayerNumber, SetterZone) = ParseLineUpKod(kod);
        }

        private static (TeamSide team, int? playerNum, int? zone) ParseLineUpKod(string kod)
        { 

            string s = kod.Trim();

            TeamSide team =
                s.StartsWith("*") ? TeamSide.Home :
                TeamSide.Away;

            s = s.TrimStart('*', 'a');

            int? player = null;
            int? zone = null;

            var mP = Regex.Match(s, @"P(\d+)>LUp", RegexOptions.IgnoreCase);
            if (mP.Success && int.TryParse(mP.Groups[1].Value, out var pnum))
                player = pnum;

            var mZ = Regex.Match(s, @"z(\d+)>LUp", RegexOptions.IgnoreCase);
            if (mZ.Success && int.TryParse(mZ.Groups[1].Value, out var znum))
                zone = znum;

            return (team, player, zone);
        }

        public override string ToString()
        {
            string who = Team == TeamSide.Home ? "HOME" :
                         Team == TeamSide.Away ? "AWAY" : "UNKNOWN";

            return $"LineUpCode[{RawCode}] Team={who}, Player={SetterPlayerNumber?.ToString() ?? "-"}, Zone={SetterZone?.ToString() ?? "-"}";
        }
    }

}
