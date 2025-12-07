namespace VolleyStatsWeb.Data
{
    public class PlayerMatchHistoryRow
    {
        public DateTime MatchDateUtc { get; set; }
        public string SeasonName { get; set; } = "";

        public bool IsHome { get; set; }
        public string HomeTeamName { get; set; } = "";
        public string AwayTeamName { get; set; } = "";
        public int HomeSetsWon { get; set; }
        public int AwaySetsWon { get; set; }

        public int Points { get; set; }
        public int AttackAttempts { get; set; }
        public int AttackPoints { get; set; }
        public int Blocks { get; set; }
        public int Aces { get; set; }
    }
}
