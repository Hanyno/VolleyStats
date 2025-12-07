using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolleyStats.DTO
{
    public class SkillTrendPointDto
    {
        public DateTime MatchDate { get; set; }
        public string OpponentName { get; set; } = string.Empty;

        public int Attempts { get; set; }
        public int Points { get; set; }
        public int Errors { get; set; }

        public double Efficiency =>
            Attempts == 0 ? 0.0 : (double)(Points - Errors) / Attempts;
    }
}
