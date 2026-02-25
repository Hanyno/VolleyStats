using System;
using System.Collections.Generic;
using System.Text;

namespace VolleyStats.Domain
{
    public class Match
    {
        public MatchMetadata Metadata { get; set; }
        public MatchInfo Info { get; set; }
        public MatchTeam HomeTeam { get; set; }
        public MatchTeam AwayTeam { get; set; }
        public MatchMoreInfo MoreInfo { get; set; }
        // TODO: try comments in dv4 and figure out how they work
        public string Comments { get; set; }

    }
}
