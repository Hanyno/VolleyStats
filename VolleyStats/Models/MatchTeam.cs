using System;
using System.Collections.Generic;
using System.Text;

namespace VolleyStats.Models
{
    public class MatchTeam
    {
        public string TeamCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int SetsWon { get; set; }
        public string? CoachName { get; set; }
        public string? AssistantCoachName { get; set; }
        public string Color { get; set; }
    }
}
