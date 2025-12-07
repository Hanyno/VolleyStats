using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolleyStats.DTO
{
    public class TeamBasicOverviewDto
    {
        public int MatchesPlayed { get; set; }
        public int MatchesWon { get; set; }
        public int MatchesLost => MatchesPlayed - MatchesWon;

        public int SetsWon { get; set; }
        public int SetsLost { get; set; }

        public double MatchWinRate =>
            MatchesPlayed == 0 ? 0.0 : (double)MatchesWon / MatchesPlayed;

        public double SetWinRate =>
            (SetsWon + SetsLost) == 0 ? 0.0 : (double)SetsWon / (SetsWon + SetsLost);
    }
}
