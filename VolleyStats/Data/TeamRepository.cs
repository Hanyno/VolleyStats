using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using VolleyStats.Domain;
using VolleyStats.Enums;

namespace VolleyStats.Data
{
    public class TeamsRepository
    {
        private readonly string _connectionString;

        public TeamsRepository()
        {
            var dbPath = DatabasePaths.GetTeamsDatabasePath();
            _connectionString = $"Data Source={dbPath}";
        }

        private SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }


        public void SaveTeam(Team team)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();

            if (team.Id == 0)
                InsertTeam(conn, team);
            else
                UpdateTeam(conn, team);

            DeletePlayersForTeam(conn, team.Id);

            foreach (var player in team.Players)
            {
                player.TeamId = team.Id;
                InsertPlayer(conn, player);
            }

            tx.Commit();
        }

        private static void InsertTeam(SqliteConnection conn, Team team)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Teams
                (TeamCode, Name, CoachName, AssistantCoachName, Abbreviation,
                 CharacterEncoding)
                VALUES
                ($code, $name, $coach, $assistant, $abbr,
                 $enc);
                SELECT last_insert_rowid();";

            cmd.Parameters.AddWithValue("$code", team.TeamCode);
            cmd.Parameters.AddWithValue("$name", team.Name);
            cmd.Parameters.AddWithValue("$coach", (object?)team.CoachName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$assistant", (object?)team.AssistantCoachName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$abbr", (object?)team.Abbreviation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$enc", (object?)team.CharacterEncoding ?? DBNull.Value);

            var id = (long)cmd.ExecuteScalar()!;
            team.Id = (int)id;
        }

        private static void UpdateTeam(SqliteConnection conn, Team team)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Teams SET
                    TeamCode = $code,
                    Name = $name,
                    CoachName = $coach,
                    AssistantCoachName = $assistant,
                    Abbreviation = $abbr,
                    CharacterEncoding = $enc
                WHERE Id = $id;
                ";

            cmd.Parameters.AddWithValue("$id", team.Id);
            cmd.Parameters.AddWithValue("$code", team.TeamCode);
            cmd.Parameters.AddWithValue("$name", team.Name);
            cmd.Parameters.AddWithValue("$coach", (object?)team.CoachName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$assistant", (object?)team.AssistantCoachName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$abbr", (object?)team.Abbreviation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$enc", (object?)team.CharacterEncoding ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        private static void DeletePlayersForTeam(SqliteConnection conn, int teamId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Players WHERE TeamId = $teamId;";
            cmd.Parameters.AddWithValue("$teamId", teamId);
            cmd.ExecuteNonQuery();
        }

        private static void InsertPlayer(SqliteConnection conn, Player player)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Players
                (TeamId, JerseyNumber, ExternalPlayerId, LastName, FirstName,
                 BirthDate, HeightCm, Position, PlayerRole, NickName,
                 IsForeign, TransferredOut, BirthDateSerial)
                VALUES
                ($teamId, $jersey, $extId, $last, $first,
                 $birth, $height, $position, $role, $nickName,
                 $foreign, $transferred, $birthSerial);
                ";

            cmd.Parameters.AddWithValue("$teamId", player.TeamId);
            cmd.Parameters.AddWithValue("$jersey", player.JerseyNumber);
            cmd.Parameters.AddWithValue("$extId", (object?)player.ExternalPlayerId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$last", (object?)player.LastName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$first", (object?)player.FirstName ?? DBNull.Value);

            cmd.Parameters.AddWithValue("$birth",
                player.BirthDate.HasValue
                    ? player.BirthDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : DBNull.Value);

            cmd.Parameters.AddWithValue("$height", (object?)player.HeightCm ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$position", (int)player.Position);
            cmd.Parameters.AddWithValue("$role", player.PlayerRole);
            cmd.Parameters.AddWithValue("$nickName", (object?)player.NickName ?? DBNull.Value);

            cmd.Parameters.AddWithValue("$foreign",
                player.IsForeign.HasValue ? (player.IsForeign.Value ? 1 : 0) : DBNull.Value);
            cmd.Parameters.AddWithValue("$transferred",
                player.TransferredOut.HasValue ? (player.TransferredOut.Value ? 1 : 0) : DBNull.Value);

            cmd.Parameters.AddWithValue("$birthSerial", (object?)player.BirthDateSerial ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public List<Team> GetAllTeamsWithPlayers()
        {
            using var conn = OpenConnection();

            var teams = new Dictionary<int, Team>();

            // Teams
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Teams;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var t = new Team
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        TeamCode = reader.GetString(reader.GetOrdinal("TeamCode")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        CoachName = ReadNullableString(reader, "CoachName"),
                        AssistantCoachName = ReadNullableString(reader, "AssistantCoachName"),
                        Abbreviation = ReadNullableString(reader, "Abbreviation"),
                        CharacterEncoding = ReadNullableInt(reader, "CharacterEncoding"),
                    };
                    teams[t.Id] = t;
                }
            }

            // Players
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Players;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var teamId = reader.GetInt32(reader.GetOrdinal("TeamId"));
                    if (!teams.TryGetValue(teamId, out var team))
                        continue;

                    var p = new Player
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        TeamId = teamId,
                        JerseyNumber = reader.GetInt32(reader.GetOrdinal("JerseyNumber")),
                        ExternalPlayerId = ReadNullableString(reader, "ExternalPlayerId") ?? string.Empty,
                        LastName = ReadNullableString(reader, "LastName"),
                        FirstName = ReadNullableString(reader, "FirstName"),
                        BirthDate = ReadNullableDate(reader, "BirthDate"),
                        HeightCm = ReadNullableInt(reader, "HeightCm"),
                        Position = (PlayerPost)(ReadNullableInt(reader, "Position") ?? 0),
                        PlayerRole = ReadNullableString(reader, "PlayerRole"),
                        NickName = ReadNullableString(reader, "NickName"),
                        IsForeign = ReadNullableBool(reader, "IsForeign"),
                        TransferredOut = ReadNullableBool(reader, "TransferredOut"),
                        BirthDateSerial = ReadNullableInt(reader, "BirthDateSerial"),
                        Team = team
                    };

                    team.Players.Add(p);
                }
            }

            return [.. teams.Values];
        }

        // ------- Helper metody pro čtení -------

        private static string? ReadNullableString(SqliteDataReader r, string col)
        {
            var idx = r.GetOrdinal(col);
            return r.IsDBNull(idx) ? null : r.GetString(idx);
        }

        private static int? ReadNullableInt(SqliteDataReader r, string col)
        {
            var idx = r.GetOrdinal(col);
            return r.IsDBNull(idx) ? null : r.GetInt32(idx);
        }

        private static bool? ReadNullableBool(SqliteDataReader r, string col)
        {
            var idx = r.GetOrdinal(col);
            if (r.IsDBNull(idx)) return null;
            var val = r.GetInt32(idx);
            return val != 0;
        }

        private static DateTime? ReadNullableDate(SqliteDataReader r, string col)
        {
            var idx = r.GetOrdinal(col);
            if (r.IsDBNull(idx)) return null;

            var s = r.GetString(idx);
            if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
                return dt;

            return null;
        }

        // ------- Smazání týmu -------

        public void DeleteTeam(int teamId)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Players WHERE TeamId = $teamId;";
                cmd.Parameters.AddWithValue("$teamId", teamId);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Teams WHERE Id = $id;";
                cmd.Parameters.AddWithValue("$id", teamId);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }
    }
}
