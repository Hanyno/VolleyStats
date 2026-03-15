using System;
using System.Collections.Generic;
using System.Text;

namespace VolleyStats.Models
{
    public class MatchMoreInfo
    {
        // first line in dvw section [3MORE]
        public string? Referees { get; set; }
        public int? Spectators { get; set; }
        public int? Receipts { get; set; }
        public string? City { get; set; }
        public string? Hall { get; set; }
        public string? Scout { get; set; }
        // second line in dvw section [3MORE]

    }
}
