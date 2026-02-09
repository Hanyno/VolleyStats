using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VolleyStats.Enums;

namespace VolleyStats.Domain
{
    public class Player
    {
        public int Id { get; set; }                                     // Database primary key

        public int TeamId { get; set; }                                 // Foreign key to the Team entity
        public int JerseyNumber { get; set; }
        public string ExternalPlayerId { get; set; } = string.Empty;    // Player ID from the original data, using by data volley (can be same in different teams, not in same team)
        public string? LastName { get; set; } = string.Empty;
        public string? FirstName { get; set; } = string.Empty;
        public DateTimeOffset? BirthDate { get; set; }
        public int? HeightCm { get; set; }
        public PlayerPost Position { get; set; }
        public string? PlayerRole { get; set; } = string.Empty;         // 'C' = Captain, ' ' = normal player, 'L' = Libero
        public string? NickName { get; set; }
        public bool? IsForeign { get; set; }
        public bool? TransferredOut { get; set; }
        public int? BirthDateSerial { get; set; }

        public Team? Team { get; set; }                                 // Navigation property to the Team entity
    }
}
