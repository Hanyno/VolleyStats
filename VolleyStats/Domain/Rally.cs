using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VolleyStats.Enums;

namespace VolleyStats.Domain
{
    public class Rally
    {
        public int Id { get; set; }

        public MatchSet Set { get; set; } = null!;
        public int SequenceNumber { get; set; }

        public TeamSide ServingSide { get; set; }

        public int HomeScoreBefore { get; set; }
        public int AwayScoreBefore { get; set; }

        public int HomeScoreAfter { get; set; }
        public int AwayScoreAfter { get; set; }

        public List<MatchEvent> Events { get; } = new();
    }
}
