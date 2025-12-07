namespace VolleyStatsWeb.Data
{
    public class PlayerBasicInfo
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? BirthDate { get; set; }

        public int? HeightCm { get; set; }
        public int? PositionCode { get; set; }

        public int JerseyNumber { get; set; }
        public string TeamName { get; set; } = "";
    }
}
