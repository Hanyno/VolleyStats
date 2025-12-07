namespace VolleyStatsWeb.DTO
{
    public class PlayerProfileDto
    {
        public int Id { get; set; }

        public string FullName { get; set; } = "";
        public int Age { get; set; }

        public int? HeightCm { get; set; }

        public string Position { get; set; } = "";
        public string TeamName { get; set; } = "";
        public int JerseyNumber { get; set; }

        public string SeasonName { get; set; } = "";

        public double PointsPerMatch { get; set; }
        public double AttackSuccessPercent { get; set; }
        public double AcesPerSet { get; set; }

        public string Last10MatchPointsJson { get; set; } = "[]";
        public string Last10MatchLabelsJson { get; set; } = "[]";
        public string SeasonOptionsHtml { get; set; } = "";
        public string MatchHistoryRowsHtml { get; set; } = "";

    }
}
