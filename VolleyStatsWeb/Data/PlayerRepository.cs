using Microsoft.Data.Sqlite;
using System.Globalization;

namespace VolleyStatsWeb.Data
{
    public class PlayerRepository : IPlayerRepository
    {
        private readonly string _connectionString;

        public PlayerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private SqliteConnection CreateConnection()
            => new SqliteConnection(_connectionString);

        public PlayerBasicInfo? GetPlayerBasicInfo(int playerId)
        {
            using var connection = CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            SELECT p.Id,
                   p.FirstName,
                   p.LastName,
                   p.BirthDate,
                   p.HeightCm,
                   p.Position,
                   p.JerseyNumber,
                   t.Name AS TeamName
            FROM Players p
            JOIN Teams t ON p.TeamId = t.Id
            WHERE p.Id = $id;
            ";
            cmd.Parameters.AddWithValue("$id", playerId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            var info = new PlayerBasicInfo
            {
                Id = reader.GetInt32(0),
                FirstName = reader["FirstName"] as string ?? "",
                LastName = reader["LastName"] as string ?? "",
                BirthDate = reader["BirthDate"] as string,
                HeightCm = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                PositionCode = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                JerseyNumber = reader.GetInt32(6),
                TeamName = reader["TeamName"] as string ?? ""
            };

            return info;
        }

        public string? GetLatestSeasonNameForPlayer(int playerId)
        {
            using var connection = CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            SELECT s.Name
            FROM MatchEvent e
            JOIN Rally r       ON e.RallyId    = r.Id
            JOIN MatchSet ms   ON r.MatchSetId = ms.Id
            JOIN Match m       ON ms.MatchId   = m.Id
            JOIN Competition c ON m.CompetitionId = c.Id
            JOIN Season s      ON c.SeasonId   = s.Id
            WHERE e.PlayerId = $pid
              AND m.IsOfficial = 1
              AND m.IsFinished = 1
            ORDER BY s.YearStart DESC, s.YearEnd DESC
            LIMIT 1;
            ";
            cmd.Parameters.AddWithValue("$pid", playerId);

            var result = cmd.ExecuteScalar();
            return result as string;
        }

        public int GetMatchesPlayed(int playerId)
        {
            using var connection = CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            SELECT COUNT(DISTINCT m.Id)
            FROM MatchEvent e
            JOIN Rally r     ON e.RallyId    = r.Id
            JOIN MatchSet ms ON r.MatchSetId = ms.Id
            JOIN Match m     ON ms.MatchId   = m.Id
            WHERE e.PlayerId = $pid
              AND m.IsOfficial = 1
              AND m.IsFinished = 1;
            ";
            cmd.Parameters.AddWithValue("$pid", playerId);

            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result ?? 0);
        }

        public int GetSetsPlayed(int playerId)
        {
            using var connection = CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            SELECT COUNT(DISTINCT ms.Id)
            FROM MatchEvent e
            JOIN Rally r     ON e.RallyId    = r.Id
            JOIN MatchSet ms ON r.MatchSetId = ms.Id
            JOIN Match m     ON ms.MatchId   = m.Id
            WHERE e.PlayerId = $pid
              AND m.IsOfficial = 1
              AND m.IsFinished = 1;
            ";
            cmd.Parameters.AddWithValue("$pid", playerId);

            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result ?? 0);
        }

        public List<(string Skill, string Eval)> GetAllEventsForPlayer(int playerId)
        {
            var list = new List<(string Skill, string Eval)>();

            using var connection = CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            SELECT e.Skill, e.Eval
            FROM MatchEvent e
            JOIN Rally r     ON e.RallyId    = r.Id
            JOIN MatchSet ms ON r.MatchSetId = ms.Id
            JOIN Match m     ON ms.MatchId   = m.Id
            WHERE e.PlayerId = $pid
              AND m.IsOfficial = 1
              AND m.IsFinished = 1;
            ";
            cmd.Parameters.AddWithValue("$pid", playerId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var skill = reader["Skill"] as string ?? "";
                var eval = reader["Eval"] as string ?? "";
                list.Add((skill, eval));
            }

            return list;
        }
        public IReadOnlyList<PlayerMatchPoints> GetLastMatchPoints(int playerId, int limit)
        {
            var list = new List<PlayerMatchPoints>();

            using var connection = CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            SELECT 
                m.Id,
                m.DateUtc,
                SUM(
                    CASE 
                        WHEN e.Skill IN ('Attack','Block','Serve')
                                AND e.Eval = 'Point' THEN 1
                        ELSE 0
                    END
                ) AS Points
            FROM MatchEvent e
            JOIN Rally r     ON e.RallyId    = r.Id
            JOIN MatchSet ms ON r.MatchSetId = ms.Id
            JOIN Match m     ON ms.MatchId   = m.Id
            WHERE e.PlayerId = $pid
                AND m.IsOfficial = 1
                AND m.IsFinished = 1
            GROUP BY m.Id, m.DateUtc
            ORDER BY m.DateUtc DESC
            LIMIT $limit;
            ";

            cmd.Parameters.AddWithValue("$pid", playerId);
            cmd.Parameters.AddWithValue("$limit", limit);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var matchId = reader.GetInt32(0);
                var dateStr = reader.GetString(1);
                var points = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);

                var date = DateTime.Parse(dateStr, CultureInfo.InvariantCulture);

                list.Add(new PlayerMatchPoints
                {
                    MatchId = matchId,
                    DateUtc = date,
                    Points = points
                });
            }

            return list;
        }

        public IReadOnlyList<string> GetSeasonsForPlayer(int playerId)
        {
            var list = new List<string>();

            using var connection = CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            SELECT DISTINCT s.Name
            FROM MatchEvent e
            JOIN Rally r        ON e.RallyId    = r.Id
            JOIN MatchSet ms    ON r.MatchSetId = ms.Id
            JOIN Match m        ON ms.MatchId   = m.Id
            JOIN Competition c  ON m.CompetitionId = c.Id
            JOIN Season s       ON c.SeasonId   = s.Id
            WHERE e.PlayerId = $pid
              AND m.IsOfficial = 1
              AND m.IsFinished = 1
            ORDER BY s.YearStart DESC, s.YearEnd DESC;
            ";
            cmd.Parameters.AddWithValue("$pid", playerId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(reader.GetString(0));
            }

            return list;
        }

        public IReadOnlyList<PlayerMatchHistoryRow> GetMatchHistoryForPlayerInSeason(
            int playerId,
            string seasonName)
        {
            var list = new List<PlayerMatchHistoryRow>();

            using var connection = CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            SELECT 
                m.Id,
                m.DateUtc,
                s.Name,
                p.TeamId,
                m.HomeTeamId,
                m.AwayTeamId,
                ht.Name AS HomeTeamName,
                at.Name AS AwayTeamName,
                -- sety
                SUM(CASE WHEN ms.HomeScore > ms.AwayScore THEN 1 ELSE 0 END) AS HomeSetsWon,
                SUM(CASE WHEN ms.AwayScore > ms.HomeScore THEN 1 ELSE 0 END) AS AwaySetsWon,
                -- body
                SUM(CASE WHEN e.Skill IN ('Attack','Block','Serve') 
                              AND e.Eval = 'Point' THEN 1 ELSE 0 END) AS Points,
                -- útoky
                SUM(CASE WHEN e.Skill = 'Attack' THEN 1 ELSE 0 END) AS AttackAttempts,
                SUM(CASE WHEN e.Skill = 'Attack' AND e.Eval = 'Point' THEN 1 ELSE 0 END) AS AttackPoints,
                -- bloky
                SUM(CASE WHEN e.Skill = 'Block' AND e.Eval = 'Point' THEN 1 ELSE 0 END) AS Blocks,
                -- esa
                SUM(CASE WHEN e.Skill = 'Serve' AND e.Eval = 'Point' THEN 1 ELSE 0 END) AS Aces
            FROM MatchEvent e
            JOIN Players p      ON e.PlayerId = p.Id
            JOIN Rally r        ON e.RallyId    = r.Id
            JOIN MatchSet ms    ON r.MatchSetId = ms.Id
            JOIN Match m        ON ms.MatchId   = m.Id
            JOIN Competition c  ON m.CompetitionId = c.Id
            JOIN Season s       ON c.SeasonId   = s.Id
            JOIN Teams ht       ON m.HomeTeamId = ht.Id
            JOIN Teams at       ON m.AwayTeamId = at.Id
            WHERE e.PlayerId = $pid
              AND s.Name = $seasonName
              AND m.IsOfficial = 1
              AND m.IsFinished = 1
            GROUP BY m.Id, m.DateUtc, s.Name, p.TeamId, m.HomeTeamId, m.AwayTeamId, ht.Name, at.Name
            ORDER BY m.DateUtc DESC;
            ";
            cmd.Parameters.AddWithValue("$pid", playerId);
            cmd.Parameters.AddWithValue("$seasonName", seasonName);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var dateStr = reader.GetString(1);
                var date = DateTime.Parse(dateStr, CultureInfo.InvariantCulture);

                var playerTeamId = reader.GetInt32(3);
                var homeTeamId = reader.GetInt32(4);
                var awayTeamId = reader.GetInt32(5);
                var isHome = playerTeamId == homeTeamId;

                list.Add(new PlayerMatchHistoryRow
                {
                    MatchDateUtc = date,
                    SeasonName = reader.GetString(2),
                    IsHome = isHome,
                    HomeTeamName = reader.GetString(6),
                    AwayTeamName = reader.GetString(7),
                    HomeSetsWon = reader.GetInt32(8),
                    AwaySetsWon = reader.GetInt32(9),
                    Points = reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
                    AttackAttempts = reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
                    AttackPoints = reader.IsDBNull(12) ? 0 : reader.GetInt32(12),
                    Blocks = reader.IsDBNull(13) ? 0 : reader.GetInt32(13),
                    Aces = reader.IsDBNull(14) ? 0 : reader.GetInt32(14)
                });
            }

            return list;
        }



    }

}
