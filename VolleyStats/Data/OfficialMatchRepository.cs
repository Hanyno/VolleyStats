using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using VolleyStats.Domain;
using VolleyStats.Enums;

namespace VolleyStats.Data
{
    public class OfficialMatchRepository
    {
        private readonly string _connectionString;

        public OfficialMatchRepository()
        {
            var dbPath = OfficialDatabaseInitializer.GetOfficialDbPath();
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath
            }.ToString();
        }

        public IEnumerable<Match> GetFinishedMatchesForTeam(
            int teamId,
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            int? competitionId = null,
            bool includeHome = true,
            bool includeAway = true,
            int? limitLastMatches = null)
        {
            if (!includeHome && !includeAway)
                return Array.Empty<Match>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();

            var sql = @"
                SELECT m.Id, m.DateUtc,

                       ht.Id AS HomeId, ht.TeamCode AS HomeCode, ht.Name AS HomeName,
                       ht.CoachName AS HomeCoach, ht.AssistantCoachName AS HomeAssistant,
                       ht.Abbreviation AS HomeAbb, ht.CharacterEncoding AS HomeEncoding,

                       at.Id AS AwayId, at.TeamCode AS AwayCode, at.Name AS AwayName,
                       at.CoachName AS AwayCoach, at.AssistantCoachName AS AwayAssistant,
                       at.Abbreviation AS AwayAbb, at.CharacterEncoding AS AwayEncoding

                FROM Match m
                JOIN Teams ht ON ht.Id = m.HomeTeamId
                JOIN Teams at ON at.Id = m.AwayTeamId
                WHERE m.IsFinished = 1
                  AND (m.HomeTeamId = $teamId OR m.AwayTeamId = $teamId)
            ";

            cmd.Parameters.AddWithValue("$teamId", teamId);

            if (fromUtc.HasValue)
            {
                sql += " AND m.DateUtc >= $fromUtc";
                cmd.Parameters.AddWithValue("$fromUtc",
                    fromUtc.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            }

            if (toUtc.HasValue)
            {
                sql += " AND m.DateUtc <= $toUtc";
                cmd.Parameters.AddWithValue("$toUtc",
                    toUtc.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            }

            if (competitionId.HasValue)
            {
                sql += " AND m.CompetitionId = $competitionId";
                cmd.Parameters.AddWithValue("$competitionId", competitionId.Value);
            }

            if (includeHome && !includeAway)
            {
                sql += " AND m.HomeTeamId = $teamId";
            }
            else if (!includeHome && includeAway)
            {
                sql += " AND m.AwayTeamId = $teamId";
            }

            sql += " ORDER BY m.DateUtc DESC";

            if (limitLastMatches.HasValue)
            {
                sql += " LIMIT $limit";
                cmd.Parameters.AddWithValue("$limit", limitLastMatches.Value);
            }

            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();
            var list = new List<Match>();

            var playersCache = new Dictionary<int, List<Player>>();

            while (reader.Read())
            {
                var matchId = reader.GetInt32(0);
                var dateUtc = reader.GetString(1);
                var startTime = DateTime.Parse(dateUtc, null, DateTimeStyles.RoundtripKind);

                var homeId = reader.GetInt32(2);
                var homeTeam = new Team
                {
                    Id = homeId,
                    TeamCode = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Name = reader.GetString(4),
                    CoachName = reader.IsDBNull(5) ? null : reader.GetString(5),
                    AssistantCoachName = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Abbreviation = reader.IsDBNull(7) ? null : reader.GetString(7),
                    CharacterEncoding = reader.IsDBNull(8) ? null : reader.GetInt32(8),

                    NameHex = null,
                    CoachNameHex = null,
                    AssistantCoachNameHex = null,
                    AbbreviationHex = null
                };

                var awayId = reader.GetInt32(9);
                var awayTeam = new Team
                {
                    Id = awayId,
                    TeamCode = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    Name = reader.GetString(11),
                    CoachName = reader.IsDBNull(12) ? null : reader.GetString(12),
                    AssistantCoachName = reader.IsDBNull(13) ? null : reader.GetString(13),
                    Abbreviation = reader.IsDBNull(14) ? null : reader.GetString(14),
                    CharacterEncoding = reader.IsDBNull(15) ? null : reader.GetInt32(15),

                    NameHex = null,
                    CoachNameHex = null,
                    AssistantCoachNameHex = null,
                    AbbreviationHex = null
                };

                homeTeam.Players = GetPlayersForTeamCached(connection, homeId, playersCache);
                awayTeam.Players = GetPlayersForTeamCached(connection, awayId, playersCache);

                var match = new Match
                {
                    Id = matchId,
                    StartTime = startTime,
                    HomeTeam = homeTeam,
                    AwayTeam = awayTeam,

                    MatchCode = "",
                    Season = "",
                    CompetitionCode = "",
                    IsFinished = true
                };

                list.Add(match);
            }

            return list;
        }

        public IEnumerable<Match> GetPlannedMatches()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT m.Id, m.DateUtc,

                       ht.Id AS HomeId, ht.TeamCode AS HomeCode, ht.Name AS HomeName,
                       ht.CoachName AS HomeCoach, ht.AssistantCoachName AS HomeAssistant,
                       ht.Abbreviation AS HomeAbb, ht.CharacterEncoding AS HomeEncoding,

                       at.Id AS AwayId, at.TeamCode AS AwayCode, at.Name AS AwayName,
                       at.CoachName AS AwayCoach, at.AssistantCoachName AS AwayAssistant,
                       at.Abbreviation AS AwayAbb, at.CharacterEncoding AS AwayEncoding

                FROM Match m
                JOIN Teams ht ON ht.Id = m.HomeTeamId
                JOIN Teams at ON at.Id = m.AwayTeamId
                WHERE m.IsFinished = 0
                ORDER BY m.DateUtc;
            ";

            using var reader = cmd.ExecuteReader();
            var list = new List<Match>();

            var playersCache = new Dictionary<int, List<Player>>();

            while (reader.Read())
            {
                var matchId = reader.GetInt32(0);
                var dateUtc = reader.GetString(1);
                var startTime = DateTime.Parse(dateUtc, null, DateTimeStyles.RoundtripKind);

                var homeId = reader.GetInt32(2);
                var homeTeam = new Team
                {
                    Id = homeId,
                    TeamCode = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Name = reader.GetString(4),
                    CoachName = reader.IsDBNull(5) ? null : reader.GetString(5),
                    AssistantCoachName = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Abbreviation = reader.IsDBNull(7) ? null : reader.GetString(7),
                    CharacterEncoding = reader.IsDBNull(8) ? null : reader.GetInt32(8),

                    NameHex = null,
                    CoachNameHex = null,
                    AssistantCoachNameHex = null,
                    AbbreviationHex = null
                };

                var awayId = reader.GetInt32(9);
                var awayTeam = new Team
                {
                    Id = awayId,
                    TeamCode = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    Name = reader.GetString(11),
                    CoachName = reader.IsDBNull(12) ? null : reader.GetString(12),
                    AssistantCoachName = reader.IsDBNull(13) ? null : reader.GetString(13),
                    Abbreviation = reader.IsDBNull(14) ? null : reader.GetString(14),
                    CharacterEncoding = reader.IsDBNull(15) ? null : reader.GetInt32(15),

                    NameHex = null,
                    CoachNameHex = null,
                    AssistantCoachNameHex = null,
                    AbbreviationHex = null
                };

                homeTeam.Players = GetPlayersForTeamCached(connection, homeId, playersCache);
                awayTeam.Players = GetPlayersForTeamCached(connection, awayId, playersCache);

                var match = new Match
                {
                    Id = matchId,
                    StartTime = startTime,
                    HomeTeam = homeTeam,
                    AwayTeam = awayTeam,

                    MatchCode = "",
                    Season = "",
                    CompetitionCode = ""
                };

                list.Add(match);
            }

            return list;
        }

        public void LoadMatchStatistics(Match match)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            match.Sets.Clear();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Id,
                           Number,
                           HomeScore,
                           AwayScore,
                           HomeTimeouts,
                           AwayTimeouts,
                           HomeSubstitutions,
                           AwaySubstitutions
                    FROM MatchSet
                    WHERE MatchId = $matchId
                    ORDER BY Number;
                ";
                cmd.Parameters.AddWithValue("$matchId", match.Id);

                using var reader = cmd.ExecuteReader();
                var sets = new List<MatchSet>();

                while (reader.Read())
                {
                    var set = new MatchSet
                    {
                        Id = reader.GetInt32(0),
                        Match = match,
                        Number = reader.GetInt32(1),
                        HomeScore = reader.GetInt32(2),
                        AwayScore = reader.GetInt32(3),

                        HomeTimeouts = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                        AwayTimeouts = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                        HomeSubstitutions = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                        AwaySubstitutions = reader.IsDBNull(7) ? 0 : reader.GetInt32(7)
                    };

                    sets.Add(set);
                }

                foreach (var set in sets)
                {
                    LoadRalliesForSet(connection, set);
                    match.Sets.Add(set);
                }
            }
        }

        private static void LoadRalliesForSet(SqliteConnection connection, MatchSet set)
        {
            set.Rallies.Clear();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id,
                       SequenceNumber,
                       ServingSide,
                       HomeScoreBefore,
                       AwayScoreBefore,
                       HomeScoreAfter,
                       AwayScoreAfter
                FROM Rally
                WHERE MatchSetId = $setId
                ORDER BY SequenceNumber;
            ";
            cmd.Parameters.AddWithValue("$setId", set.Id);

            using var reader = cmd.ExecuteReader();
            var rallies = new List<Rally>();

            while (reader.Read())
            {
                var rally = new Rally
                {
                    Id = reader.GetInt32(0),
                    Set = set,
                    SequenceNumber = reader.GetInt32(1),
                    ServingSide = (TeamSide)reader.GetInt32(2),
                    HomeScoreBefore = reader.GetInt32(3),
                    AwayScoreBefore = reader.GetInt32(4),
                    HomeScoreAfter = reader.GetInt32(5),
                    AwayScoreAfter = reader.GetInt32(6)
                };

                rallies.Add(rally);
            }

            foreach (var r in rallies)
            {
                LoadEventsForRally(connection, r);
                set.Rallies.Add(r);
            }
        }

        private static void LoadEventsForRally(SqliteConnection connection, Rally rally)
        {
            rally.Events.Clear();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id,
                       OrderInRally,
                       Side,
                       Skill,
                       Eval,
                       RawCode,
                       PlayerId
                FROM MatchEvent
                WHERE RallyId = $rallyId
                ORDER BY OrderInRally;
            ";
            cmd.Parameters.AddWithValue("$rallyId", rally.Id);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var ev = new MatchEvent
                {
                    Id = reader.GetInt32(0),
                    Set = rally.Set,
                    Rally = rally,
                    OrderInRally = reader.GetInt32(1),
                    Side = (TeamSide)reader.GetInt32(2),
                    Skill = Enum.Parse<BasicSkill>(reader.GetString(3)),
                    Eval = Enum.Parse<EvaluationSymbol>(reader.GetString(4)),
                    RawCode = reader.IsDBNull(5) ? null : reader.GetString(5),

                    PlayerId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6)
                };

                rally.Events.Add(ev);
            }
        }


        private static List<Player> GetPlayersForTeamCached(
            SqliteConnection connection,
            int teamId,
            Dictionary<int, List<Player>> cache)
        {
            if (cache.TryGetValue(teamId, out var players))
            {
                return players;
            }

            players = LoadPlayersForTeam(connection, teamId);
            cache[teamId] = players;
            return players;
        }

        private static List<Player> LoadPlayersForTeam(SqliteConnection connection, int teamId)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id,
                       TeamId,
                       JerseyNumber,
                       ExternalPlayerId,
                       LastName,
                       FirstName,
                       BirthDate,
                       HeightCm,
                       Position,
                       PlayerRole,
                       NickName,
                       IsForeign,
                       TransferredOut,
                       BirthDateSerial
                FROM Players
                WHERE TeamId = $teamId
                ORDER BY JerseyNumber;
            ";
            cmd.Parameters.AddWithValue("$teamId", teamId);

            using var reader = cmd.ExecuteReader();
            var players = new List<Player>();

            while (reader.Read())
            {
                var player = new Player
                {
                    Id = ReadInt(reader, "Id"),
                    TeamId = ReadInt(reader, "TeamId"),
                    JerseyNumber = ReadInt(reader, "JerseyNumber"),
                    ExternalPlayerId = ReadNullableString(reader, "ExternalPlayerId"),
                    LastName = ReadString(reader, "LastName"),
                    FirstName = ReadString(reader, "FirstName"),
                    BirthDate = ReadNullableString(reader, "BirthDate") is string bd
                        ? DateTime.ParseExact(bd, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : (DateTime?)null,
                    HeightCm = ReadNullableInt(reader, "HeightCm"),
                    Position = (PlayerPost)(ReadNullableInt(reader, "Position") ?? 0),
                    PlayerRole = ReadNullableString(reader, "PlayerRole"),
                    NickName = ReadNullableString(reader, "NickName"),
                    IsForeign = ReadNullableBool(reader, "IsForeign"),
                    TransferredOut = ReadNullableBool(reader, "TransferredOut"),
                    BirthDateSerial = ReadNullableInt(reader, "BirthDateSerial")
                };

                players.Add(player);
            }

            return players;
        }

        public void SaveMatchStatistics(Match match)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var tx = connection.BeginTransaction();

            try
            {
                DeleteExistingMatchData(connection, match.Id);

                foreach (var set in match.Sets)
                {
                    var setId = InsertMatchSet(connection, match.Id, set);

                    foreach (var rally in set.Rallies)
                    {
                        var rallyId = InsertRally(connection, setId, rally);

                        foreach (var ev in rally.Events)
                        {
                            InsertMatchEvent(connection, rallyId, ev);
                        }
                    }
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        UPDATE ""Match""
                        SET IsFinished = $isFinished
                        WHERE Id = $matchId;
                    ";

                    cmd.Parameters.AddWithValue("$isFinished", match.IsFinished ? 1 : 0);
                    cmd.Parameters.AddWithValue("$matchId", match.Id);

                    cmd.ExecuteNonQuery();
                }


                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        private static void DeleteExistingMatchData(SqliteConnection connection, int matchId)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                    -- Smazat eventy
                    DELETE FROM MatchEvent
                    WHERE RallyId IN (
                        SELECT r.Id
                        FROM Rally r
                        JOIN MatchSet ms ON ms.Id = r.MatchSetId
                        WHERE ms.MatchId = $matchId
                    );

                    -- Smazat rallye
                    DELETE FROM Rally
                    WHERE MatchSetId IN (
                        SELECT Id FROM MatchSet WHERE MatchId = $matchId
                    );

                    -- Smazat sety
                    DELETE FROM MatchSet
                    WHERE MatchId = $matchId;
                ";
            cmd.Parameters.AddWithValue("$matchId", matchId);
            cmd.ExecuteNonQuery();
        }

        private static int InsertMatchSet(SqliteConnection connection, int matchId, MatchSet set)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO MatchSet (
                    MatchId,
                    Number,
                    HomeScore,
                    AwayScore,
                    HomeTimeouts,
                    AwayTimeouts,
                    HomeSubstitutions,
                    AwaySubstitutions
                )
                VALUES (
                    $matchId,
                    $number,
                    $homeScore,
                    $awayScore,
                    $homeTimeouts,
                    $awayTimeouts,
                    $homeSubs,
                    $awaySubs
                );
            ";

            cmd.Parameters.AddWithValue("$matchId", matchId);
            cmd.Parameters.AddWithValue("$number", set.Number);
            cmd.Parameters.AddWithValue("$homeScore", set.HomeScore);
            cmd.Parameters.AddWithValue("$awayScore", set.AwayScore);

            cmd.Parameters.AddWithValue("$homeTimeouts", set.HomeTimeouts);
            cmd.Parameters.AddWithValue("$awayTimeouts", set.AwayTimeouts);
            cmd.Parameters.AddWithValue("$homeSubs", set.HomeSubstitutions);
            cmd.Parameters.AddWithValue("$awaySubs", set.AwaySubstitutions);

            cmd.ExecuteNonQuery();

            cmd.CommandText = "SELECT last_insert_rowid();";
            cmd.Parameters.Clear();
            var setId = Convert.ToInt32(cmd.ExecuteScalar());
            return setId;
        }


        private static int InsertRally(SqliteConnection connection, int matchSetId, Rally rally)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                    INSERT INTO Rally (
                        MatchSetId, SequenceNumber, ServingSide,
                        HomeScoreBefore, AwayScoreBefore,
                        HomeScoreAfter, AwayScoreAfter
                    )
                    VALUES (
                        $matchSetId, $seq, $servingSide,
                        $homeBefore, $awayBefore,
                        $homeAfter, $awayAfter
                    );
                ";

            cmd.Parameters.AddWithValue("$matchSetId", matchSetId);
            cmd.Parameters.AddWithValue("$seq", rally.SequenceNumber);
            cmd.Parameters.AddWithValue("$servingSide", (int)rally.ServingSide); // 0 = Home, 1 = Away
            cmd.Parameters.AddWithValue("$homeBefore", rally.HomeScoreBefore);
            cmd.Parameters.AddWithValue("$awayBefore", rally.AwayScoreBefore);
            cmd.Parameters.AddWithValue("$homeAfter", rally.HomeScoreAfter);
            cmd.Parameters.AddWithValue("$awayAfter", rally.AwayScoreAfter);

            cmd.ExecuteNonQuery();

            cmd.CommandText = "SELECT last_insert_rowid();";
            cmd.Parameters.Clear();
            var rallyId = Convert.ToInt32(cmd.ExecuteScalar());
            return rallyId;
        }

        private static void InsertMatchEvent(SqliteConnection connection, int rallyId, MatchEvent ev)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO MatchEvent (
                    RallyId,
                    OrderInRally,
                    Side,
                    TeamId,
                    PlayerId,
                    Skill,
                    Eval,
                    RawCode
                )
                VALUES (
                    $rallyId,
                    $orderInRally,
                    $side,
                    $teamId,
                    $playerId,
                    $skill,
                    $eval,
                    $rawCode
                );
            ";

            cmd.Parameters.AddWithValue("$rallyId", rallyId);
            cmd.Parameters.AddWithValue("$orderInRally", ev.OrderInRally);

            cmd.Parameters.AddWithValue("$side", (int)ev.Side);

            var teamId = ev.Side == TeamSide.Home
                ? ev.Set.Match.HomeTeam.Id
                : ev.Set.Match.AwayTeam.Id;
            cmd.Parameters.AddWithValue("$teamId", teamId);

            int? playerId = null;

            if (ev.Player != null)
            {
                playerId = ev.Player.Player.Id;

                if (ev.Player.Player != null)
                    playerId = ev.Player.Player.Id;
            }

            if (playerId.HasValue)
                cmd.Parameters.AddWithValue("$playerId", playerId.Value);
            else
                cmd.Parameters.AddWithValue("$playerId", DBNull.Value);

            cmd.Parameters.AddWithValue("$skill", ev.Skill.ToString());
            cmd.Parameters.AddWithValue("$eval", ev.Eval.ToString());

            cmd.Parameters.AddWithValue("$rawCode",
                (object?)ev.RawCode ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public void UploadTeams(IEnumerable<Team> teams)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var tx = connection.BeginTransaction();

            foreach (var team in teams)
            {
                var teamId = EnsureTeam(connection, tx, team);

                DeletePlayersForTeam(connection, tx, teamId);

                InsertPlayersForTeam(connection, tx, teamId, team.Players);
            }

            tx.Commit();
        }

        private static int EnsureTeam(
            SqliteConnection connection,
            SqliteTransaction tx,
            Team team)
        {
            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.Transaction = tx;
                checkCmd.CommandText = @"
                    SELECT Id
                    FROM Teams
                    WHERE TeamCode = $code;
                ";
                checkCmd.Parameters.AddWithValue("$code", team.TeamCode);

                var result = checkCmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    var existingId = Convert.ToInt32(result);

                    using var updateCmd = connection.CreateCommand();
                    updateCmd.Transaction = tx;
                    updateCmd.CommandText = @"
                        UPDATE Teams
                        SET Name               = $name,
                            CoachName          = $coach,
                            AssistantCoachName = $assistant,
                            Abbreviation       = $abbr,
                            CharacterEncoding  = $enc
                        WHERE Id = $id;
                    ";

                    updateCmd.Parameters.AddWithValue("$id", existingId);
                    updateCmd.Parameters.AddWithValue("$name", team.Name);
                    updateCmd.Parameters.AddWithValue("$coach", (object?)team.CoachName ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("$assistant", (object?)team.AssistantCoachName ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("$abbr", (object?)team.Abbreviation ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("$enc", (object?)team.CharacterEncoding ?? DBNull.Value);

                    updateCmd.ExecuteNonQuery();

                    return existingId;
                }
            }

            using (var insertCmd = connection.CreateCommand())
            {
                insertCmd.Transaction = tx;
                insertCmd.CommandText = @"
                    INSERT INTO Teams (
                        TeamCode,
                        Name,
                        CoachName,
                        AssistantCoachName,
                        Abbreviation,
                        CharacterEncoding
                    )
                    VALUES (
                        $code,
                        $name,
                        $coach,
                        $assistant,
                        $abbr,
                        $enc
                    );

                    SELECT last_insert_rowid();
                ";

                insertCmd.Parameters.AddWithValue("$code", team.TeamCode);
                insertCmd.Parameters.AddWithValue("$name", team.Name);
                insertCmd.Parameters.AddWithValue("$coach", (object?)team.CoachName ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("$assistant", (object?)team.AssistantCoachName ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("$abbr", (object?)team.Abbreviation ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("$enc", (object?)team.CharacterEncoding ?? DBNull.Value);

                var newId = (long)insertCmd.ExecuteScalar();
                return (int)newId;
            }
        }

        private static void DeletePlayersForTeam(
            SqliteConnection connection,
            SqliteTransaction tx,
            int teamId)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                DELETE FROM Players
                WHERE TeamId = $teamId;
            ";
            cmd.Parameters.AddWithValue("$teamId", teamId);
            cmd.ExecuteNonQuery();
        }

        private static void InsertPlayersForTeam(
            SqliteConnection connection,
            SqliteTransaction tx,
            int teamId,
            IEnumerable<Player> players)
        {
            foreach (var p in players)
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;

                cmd.CommandText = @"
                    INSERT INTO Players (
                        TeamId,
                        JerseyNumber,
                        ExternalPlayerId,
                        LastName,
                        FirstName,
                        BirthDate,
                        HeightCm,
                        Position,
                        PlayerRole,
                        NickName,
                        IsForeign,
                        TransferredOut,
                        BirthDateSerial
                    )
                    VALUES (
                        $teamId,
                        $jersey,
                        $externalId,
                        $lastName,
                        $firstName,
                        $birthDate,
                        $height,
                        $position,
                        $playerRole,
                        $nick,
                        $isForeign,
                        $transferredOut,
                        $birthSerial
                    );
                ";

                cmd.Parameters.AddWithValue("$teamId", teamId);

                cmd.Parameters.AddWithValue("$jersey", (object?)p.JerseyNumber ?? 0);

                cmd.Parameters.AddWithValue("$externalId", (object?)p.ExternalPlayerId ?? DBNull.Value);

                cmd.Parameters.AddWithValue("$lastName", (object?)p.LastName ?? "");
                cmd.Parameters.AddWithValue("$firstName", (object?)p.FirstName ?? "");

                cmd.Parameters.AddWithValue("$birthDate",
                p.BirthDate.HasValue
                    ? p.BirthDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : DBNull.Value);
                cmd.Parameters.AddWithValue("$height", (object?)p.HeightCm ?? DBNull.Value);

                cmd.Parameters.AddWithValue("$position", p.Position);

                cmd.Parameters.AddWithValue("$playerRole", p.PlayerRole);
                cmd.Parameters.AddWithValue("$nick", DBNull.Value);
                cmd.Parameters.AddWithValue("$isForeign", DBNull.Value);
                cmd.Parameters.AddWithValue("$transferredOut", DBNull.Value);
                cmd.Parameters.AddWithValue("$birthSerial", DBNull.Value);

                cmd.ExecuteNonQuery();
            }
        }

        public void UploadMatches(IEnumerable<Match> matches)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var tx = connection.BeginTransaction();

            var competitionId = EnsureDefaultCompetition(connection, tx);

            foreach (var match in matches)
            {
                InsertMatchIfNotExists(connection, tx, competitionId, match);
            }

            tx.Commit();
        }

        private static int EnsureDefaultSeason(SqliteConnection connection, SqliteTransaction tx)
        {
            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.Transaction = tx;
                checkCmd.CommandText = @"
                    SELECT Id
                    FROM Season
                    WHERE Name = $name;
                ";
                checkCmd.Parameters.AddWithValue("$name", "Imported Season");

                var result = checkCmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
            }

            var year = DateTime.UtcNow.Year;

            using (var insertCmd = connection.CreateCommand())
            {
                insertCmd.Transaction = tx;
                insertCmd.CommandText = @"
            INSERT INTO Season (Name, YearStart, YearEnd)
            VALUES ($name, $yearStart, $yearEnd);
            SELECT last_insert_rowid();
        ";

                insertCmd.Parameters.AddWithValue("$name", "Imported Season");
                insertCmd.Parameters.AddWithValue("$yearStart", year);
                insertCmd.Parameters.AddWithValue("$yearEnd", year);

                var newId = (long)insertCmd.ExecuteScalar();
                return (int)newId;
            }
        }

        private static int EnsureDefaultCompetition(SqliteConnection connection, SqliteTransaction tx)
        {
            var seasonId = EnsureDefaultSeason(connection, tx);

            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.Transaction = tx;
                checkCmd.CommandText = @"
                    SELECT Id
                    FROM Competition
                    WHERE SeasonId = $seasonId AND Name = $name;
                ";
                checkCmd.Parameters.AddWithValue("$seasonId", seasonId);
                checkCmd.Parameters.AddWithValue("$name", "Imported Competition");

                var result = checkCmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
            }

            using (var insertCmd = connection.CreateCommand())
            {
                insertCmd.Transaction = tx;
                insertCmd.CommandText = @"
                    INSERT INTO Competition (SeasonId, Name, Level, Gender)
                    VALUES ($seasonId, $name, $level, $gender);
                    SELECT last_insert_rowid();
                ";

                insertCmd.Parameters.AddWithValue("$seasonId", seasonId);
                insertCmd.Parameters.AddWithValue("$name", "Imported Competition");
                insertCmd.Parameters.AddWithValue("$level", DBNull.Value);
                insertCmd.Parameters.AddWithValue("$gender", DBNull.Value);

                var newId = (long)insertCmd.ExecuteScalar();
                return (int)newId;
            }
        }

        private static void InsertMatchIfNotExists(
            SqliteConnection connection,
            SqliteTransaction tx,
            int competitionId,
            Match match)
        {
            if (match.HomeTeam == null || match.AwayTeam == null)
                throw new InvalidOperationException("Match must have HomeTeam and AwayTeam set.");

            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.Transaction = tx;
                checkCmd.CommandText = @"
            SELECT Id
            FROM Match
            WHERE CompetitionId = $competitionId
              AND DateUtc = $dateUtc
              AND HomeTeamId = $homeTeamId
              AND AwayTeamId = $awayTeamId;
        ";

                var dateUtc = match.StartTime.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

                checkCmd.Parameters.AddWithValue("$competitionId", competitionId);
                checkCmd.Parameters.AddWithValue("$dateUtc", dateUtc);
                checkCmd.Parameters.AddWithValue("$homeTeamId", match.HomeTeam.Id);
                checkCmd.Parameters.AddWithValue("$awayTeamId", match.AwayTeam.Id);

                var existing = checkCmd.ExecuteScalar();
                if (existing != null && existing != DBNull.Value)
                {
                    match.Id = Convert.ToInt32(existing);
                    return;
                }
            }

            using (var insertCmd = connection.CreateCommand())
            {
                insertCmd.Transaction = tx;
                insertCmd.CommandText = @"
                    INSERT INTO Match (
                        CompetitionId,
                        DateUtc,
                        HomeTeamId,
                        AwayTeamId,
                        IsOfficial,
                        IsFinished
                    )
                    VALUES (
                        $competitionId,
                        $dateUtc,
                        $homeTeamId,
                        $awayTeamId,
                        1,
                        0
                    );
                    SELECT last_insert_rowid();
                ";

                var dateUtc = match.StartTime.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

                insertCmd.Parameters.AddWithValue("$competitionId", competitionId);
                insertCmd.Parameters.AddWithValue("$dateUtc", dateUtc);
                insertCmd.Parameters.AddWithValue("$homeTeamId", match.HomeTeam.Id);
                insertCmd.Parameters.AddWithValue("$awayTeamId", match.AwayTeam.Id);

                var newId = (long)insertCmd.ExecuteScalar();
                match.Id = (int)newId;
            }
        }

        public IEnumerable<Team> GetAllTeams()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id,
                       TeamCode,
                       Name,
                       CoachName,
                       AssistantCoachName,
                       Abbreviation,
                       CharacterEncoding
                FROM Teams
                ORDER BY Name;
            ";

            using var reader = cmd.ExecuteReader();
            var list = new List<Team>();

            while (reader.Read())
            {
                var team = new Team
                {
                    Id = ReadInt(reader, "Id"),
                    TeamCode = ReadString(reader, "TeamCode"),
                    Name = ReadString(reader, "Name"),
                    CoachName = ReadNullableString(reader, "CoachName"),
                    AssistantCoachName = ReadNullableString(reader, "AssistantCoachName"),
                    Abbreviation = ReadNullableString(reader, "Abbreviation"),
                    CharacterEncoding = ReadNullableInt(reader, "CharacterEncoding")
                };

                list.Add(team);
            }

            return list;
        }



        private static string ReadString(SqliteDataReader r, string column)
    => r[column] == DBNull.Value ? "" : (string)r[column];

        private static string? ReadNullableString(SqliteDataReader r, string column)
            => r[column] == DBNull.Value ? null : (string)r[column];

        private static int ReadInt(SqliteDataReader r, string column)
            => Convert.ToInt32(r[column]);

        private static int? ReadNullableInt(SqliteDataReader r, string column)
            => r[column] == DBNull.Value ? null : Convert.ToInt32(r[column]);

        private static bool? ReadNullableBool(SqliteDataReader r, string column)
            => r[column] == DBNull.Value ? null : Convert.ToInt32(r[column]) == 1;

    }
}
