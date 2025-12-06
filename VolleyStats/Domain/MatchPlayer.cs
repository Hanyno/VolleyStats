using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VolleyStats.Enums;

namespace VolleyStats.Domain
{
    /// <summary>
    /// Jeden konkrétní hráč v kontextu zápasu (včetně čísla dresu atd.)
    /// </summary>
    public class MatchPlayer
    {
        public int Id { get; set; }

        public Match Match { get; set; } = null!;
        public TeamSide Side { get; set; }

        public Team Team { get; set; } = null!;
        public Player Player { get; set; } = null!;

        public int ShirtNumber { get; set; }
        public bool IsLibero { get; set; }

        public PlayerPost DefaultPosition { get; set; }
    }

}
