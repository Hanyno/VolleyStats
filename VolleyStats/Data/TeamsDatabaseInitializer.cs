using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolleyStats.Data
{
    public static class TeamsDatabaseInitializer
    {
        public static void EnsureCreated()
        {
            var dbPath = DatabasePaths.GetTeamsDatabasePath();
            _ = !File.Exists(dbPath);

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Teams (
                    Id                      INTEGER PRIMARY KEY AUTOINCREMENT,
                    TeamCode                TEXT    NOT NULL,
                    Name                    TEXT    NOT NULL,
                    CoachName               TEXT,
                    AssistantCoachName      TEXT,
                    Abbreviation            TEXT,
                    CharacterEncoding       INTEGER
                );

                CREATE TABLE IF NOT EXISTS Players (
                    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                    TeamId          INTEGER NOT NULL,

                    JerseyNumber    INTEGER NOT NULL,
                    ExternalPlayerId TEXT,
                    LastName        TEXT,
                    FirstName       TEXT,
                    BirthDate       TEXT,      -- uložené jako 'yyyy-MM-dd'
                    HeightCm        INTEGER,
                    Position        INTEGER,   -- enum PlayerPost
                    PlayerRole      TEXT,      -- char: 'C', 'L', ' ' atd.
                    NickName        TEXT,
                    IsForeign       INTEGER,   -- NULL / 0 / 1
                    TransferredOut  INTEGER,   -- NULL / 0 / 1
                    BirthDateSerial INTEGER,

                    FOREIGN KEY (TeamId) REFERENCES Teams(Id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS IX_Players_TeamId ON Players(TeamId);
                ";
            cmd.ExecuteNonQuery();
        }
    }
}

