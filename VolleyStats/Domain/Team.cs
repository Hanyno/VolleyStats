using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolleyStats.Domain
{
    public class Team
    {
        public string TeamCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? CoachName { get; set; }
        public string? AssistantCoachName { get; set; }

        public string? Abbreviation { get; set; }

        public int? CharacterEncoding { get; set; }

        public List<Player> Players { get; set; } = new();
    }
}
