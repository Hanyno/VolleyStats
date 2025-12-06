using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolleyStats.Domain
{
    public class Match
    {
        public int Id { get; set; }

        public string MatchCode { get; set; } = "";
        public string Season { get; set; } = "";
        public string CompetitionCode { get; set; } = "";
        public DateTime StartTime { get; set; }

        public Team HomeTeam { get; set; } = null!;
        public Team AwayTeam { get; set; } = null!;

        public bool IsFinished { get; set; } = false;

        public List<MatchPlayer> Players { get; } = new();

        public List<MatchSet> Sets { get; } = new();
    }

}
