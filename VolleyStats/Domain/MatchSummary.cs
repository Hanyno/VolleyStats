using System;

namespace VolleyStats.Domain
{
    public class MatchSummary
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? Season { get; set; }
        public string HomeTeam { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;
        public int? HomeSets { get; set; }
        public int? AwaySets { get; set; }
        public DateOnly? Date { get; set; }
        public TimeOnly? Time { get; set; }
        public string? League { get; set; }
        public string? Phase { get; set; }
    }
}
