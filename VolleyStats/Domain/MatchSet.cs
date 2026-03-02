using System;
using System.Collections.Generic;
using System.Text;

namespace VolleyStats.Domain
{
    public class MatchSet
    {
        public bool Idk { get; set; }
        public List<MatchScore>? PartialScores { get; set; }
        public MatchScore? FinalScore { get; set; }
        public int Duration { get; set; }
    }
}
