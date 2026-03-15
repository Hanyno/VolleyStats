using System;
using System.Collections.Generic;
using System.Text;

namespace VolleyStats.Models
{
    public class Match
    {
        public MatchMetadata Metadata { get; set; }
        public MatchInfo Info { get; set; }
        public MatchTeam HomeTeam { get; set; }
        public MatchTeam AwayTeam { get; set; }
        public MatchMoreInfo MoreInfo { get; set; }
        public MatchComments Comments { get; set; }
        public List<MatchSet> Sets { get; set; }
        public List<MatchPlayer> HomePlayers { get; set; }
        public List<MatchPlayer> AwayPlayers { get; set; }
        public List<AttackCombination> AttackCombinations { get; set; }
        public List<SetterCall> SetterCalls { get; set; } = new List<SetterCall>();
        public WinningSymbols WinningSymbols { get; set; }
        public string Reserve { get; set; }
        public List<string> VideoPaths { get; set; }
        public List<Code> ScoutCodes { get; set; }
    }
}
