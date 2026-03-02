using System;
using System.Collections.Generic;
using System.Text;
using VolleyStats.Enums;

namespace VolleyStats.Domain
{
    public class MatchPlayer
    {
        public bool IsHome { get; set; }
        public int JerseyNumber { get; set; }
        public int RandomId { get; set; }
        public List<string> StartingZones { get; set; }
        public string ExternalPlayerId { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public string? FirstName { get; set; } = string.Empty;
        public DateTimeOffset? BirthDate { get; set; }
        public int? HeightCm { get; set; }
        public PlayerPost Position { get; set; }
        public string? PlayerRole { get; set; } = string.Empty;
        public string? NickName { get; set; }
        public bool? IsForeign { get; set; }
        public bool? TransferredOut { get; set; }
        public int? BirthDateSerial { get; set; }

    }
}
