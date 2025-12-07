using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolleyStats.Data
{
    public static class OfficialDatabaseInitializer
    {
        public static string GetOfficialDbPath()
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            return System.IO.Path.Combine(desktop, "official_stats.db");
        }

        public static void EnsureCreated()
        {
            var dbPath = GetOfficialDbPath();
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = SchemaSql;
            command.ExecuteNonQuery();
        }

        private const string SchemaSql = @"
            CREATE TABLE IF NOT EXISTS Season (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Name      TEXT NOT NULL,
                YearStart INTEGER NOT NULL,
                YearEnd   INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Competition (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                SeasonId  INTEGER NOT NULL,
                Name      TEXT NOT NULL,
                Level     TEXT,
                Gender    TEXT,
                FOREIGN KEY (SeasonId) REFERENCES Season(Id)
            );

            -- Teams podle lokální DB
            CREATE TABLE IF NOT EXISTS Teams (
                Id                      INTEGER PRIMARY KEY AUTOINCREMENT,
                TeamCode                TEXT    NOT NULL,
                Name                    TEXT    NOT NULL,
                CoachName               TEXT,
                AssistantCoachName      TEXT,
                Abbreviation            TEXT,
                CharacterEncoding       INTEGER
            );

            -- Players podle lokální DB
            CREATE TABLE IF NOT EXISTS Players (
                Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                TeamId           INTEGER NOT NULL,

                JerseyNumber     INTEGER NOT NULL,
                ExternalPlayerId TEXT,
                LastName         TEXT,
                FirstName        TEXT,
                BirthDate        TEXT,      -- uložené jako 'yyyy-MM-dd'
                HeightCm         INTEGER,
                Position         INTEGER,   -- enum PlayerPost
                PlayerRole       TEXT,      -- char: 'C', 'L', ' ' atd.
                NickName         TEXT,
                IsForeign        INTEGER,   -- NULL / 0 / 1
                TransferredOut   INTEGER,   -- NULL / 0 / 1
                BirthDateSerial  INTEGER,

                FOREIGN KEY (TeamId) REFERENCES Teams(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_Players_TeamId ON Players(TeamId);

            CREATE TABLE IF NOT EXISTS Match (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                CompetitionId INTEGER NOT NULL,
                DateUtc       TEXT NOT NULL,
                HomeTeamId    INTEGER NOT NULL,
                AwayTeamId    INTEGER NOT NULL,
                IsOfficial    INTEGER NOT NULL DEFAULT 1,
                IsFinished    INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (CompetitionId) REFERENCES Competition(Id),
                FOREIGN KEY (HomeTeamId) REFERENCES Teams(Id),
                FOREIGN KEY (AwayTeamId) REFERENCES Teams(Id)
            );

            CREATE TABLE IF NOT EXISTS MatchSet (
                Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                MatchId             INTEGER NOT NULL,
                Number              INTEGER NOT NULL,
                HomeScore           INTEGER NOT NULL,
                AwayScore           INTEGER NOT NULL,
                HomeTimeouts        INTEGER NOT NULL,
                AwayTimeouts        INTEGER NOT NULL,
                HomeSubstitutions   INTEGER NOT NULL,
                AwaySubstitutions   INTEGER NOT NULL,
                FOREIGN KEY (MatchId) REFERENCES Match(Id)
            );

            CREATE TABLE IF NOT EXISTS Rally (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                MatchSetId      INTEGER NOT NULL,
                SequenceNumber  INTEGER NOT NULL,
                ServingSide     INTEGER NOT NULL, -- 0 = Home, 1 = Away
                HomeScoreBefore INTEGER NOT NULL,
                AwayScoreBefore INTEGER NOT NULL,
                HomeScoreAfter  INTEGER NOT NULL,
                AwayScoreAfter  INTEGER NOT NULL,
                FOREIGN KEY (MatchSetId) REFERENCES MatchSet(Id)
            );

            CREATE TABLE IF NOT EXISTS MatchEvent (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,

                RallyId     INTEGER NOT NULL,
                OrderInRally INTEGER NOT NULL,

                Side        INTEGER NOT NULL,

                TeamId      INTEGER NOT NULL,
                PlayerId    INTEGER,

                Skill       TEXT NOT NULL,
                Eval        TEXT NOT NULL,

                RawCode     TEXT,

                FOREIGN KEY (RallyId)  REFERENCES Rally(Id),
                FOREIGN KEY (TeamId)   REFERENCES Teams(Id),
                FOREIGN KEY (PlayerId) REFERENCES Players(Id)
            );
            ";
    }
}

