using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolleyStats.DTO
{
    public class PlayerStatsDto
    {
        public int JerseyNumber { get; set; }

        public string PlayerName { get; set; } = string.Empty;

        public int AttackAttempts { get; set; }
        public int AttackPoints { get; set; }
        public int AttackErrors { get; set; }
        public double AttackEfficiency { get; set; }

        public int ServeAttempts { get; set; }
        public int ServePoints { get; set; }
        public int ServeErrors { get; set; }
        public double ServeEfficiency { get; set; }

        public int ReceptionAttempts { get; set; }
        public int ReceptionPositive { get; set; }
        public int ReceptionNegative { get; set; }
        public int ReceptionErrors { get; set; }
        public double ReceptionEfficiency { get; set; }
    }
}
