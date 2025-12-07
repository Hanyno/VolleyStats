using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolleyStats.Domain
{
    public class MatchSet
    {
        public int Id { get; set; }

        public Match Match { get; set; } = null!;
        public int Number { get; set; }

        public int HomeScore { get; set; }
        public int AwayScore { get; set; }

        public int HomeTimeouts { get; set; }
        public int AwayTimeouts { get; set; }
        public int HomeSubstitutions { get; set; }
        public int AwaySubstitutions { get; set; }

        public List<Rally> Rallies { get; } = new();
    }
}
