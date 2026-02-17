using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using VolleyStats.Domain;
using VolleyStats.Enums;

namespace VolleyStats.Data.Repositories
{
    public class TeamsRepository
    {
        private readonly string _dbPath;

        public TeamsRepository()
        {
            _dbPath = DatabasePaths.GetTeamsDatabasePath();
            EnsureDatabase();
        }

        private VolleyStatsDbContext CreateDb() => new VolleyStatsDbContext(_dbPath);

        private void EnsureDatabase()
        {
            using var db = CreateDb();
            db.Database.EnsureCreated();
        }

        public bool TryImportFromSq(string filePath, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                errorMessage = "Soubor neexistuje.";
                return false;
            }

            var lines = ReadSqLines(filePath);
            if (!TryParseTeamFromSq(lines, out var team, out errorMessage))
            {
                return false;
            }

            using var db = CreateDb();
            db.Database.EnsureCreated();

            var existing = db.Teams
                .Include(t => t.Players)
                .FirstOrDefault(t => t.TeamCode == team.TeamCode);

            if (existing != null)
            {
                team.Id = existing.Id;
            }

            SaveTeam(team);
            return true;
        }
        //TODO: doesnt work properly with data volley, fix later
        public bool ExportTeamToSq(Team team, string filePath)
        {
            if (team == null) return false;

            var lines = BuildSqLines(team);

            var encoding = Encoding.UTF8;
            if (team.CharacterEncoding.HasValue && team.CharacterEncoding.Value == 1250)
            {
                encoding = Encoding.GetEncoding(1250);
            }

            File.WriteAllLines(filePath, lines, encoding);
            return true;
        }

        private bool TryParseTeamFromSq(string[] lines, out Team team, out string errorMessage)
        {
            team = null!;
            errorMessage = string.Empty;

            try
            {
                if (lines == null || lines.Length < 2)
                {
                    errorMessage = "Soubor neobsahuje dostatek řádků (chybí hlavička týmu).";
                    return false;
                }

                var teamLine = lines[1];
                var teamParts = teamLine.Split('\t');

                if (teamParts.Length < 5)
                {
                    errorMessage = "Řádek s týmem nemá očekávaný formát (méně než 5 sloupců).";
                    return false;
                }

                team = new Team
                {
                    TeamCode = teamParts[0].Trim(),
                    Name = teamParts[1].Trim(),
                    CoachName = teamParts.ElementAtOrDefault(2)?.Trim(),
                    AssistantCoachName = teamParts.ElementAtOrDefault(3)?.Trim(),
                    Abbreviation = teamParts.ElementAtOrDefault(4)?.Trim()
                };

                if (int.TryParse(teamParts.ElementAtOrDefault(5)?.Trim(), out var encodingCode))
                {
                    team.CharacterEncoding = encodingCode;
                }

                team.Players ??= new List<Player>();

                for (int i = 2; i < lines.Length; i++)
                {
                    var line = lines[i];

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split('\t');

                    if (parts.Length < 5)
                        continue;

                    var player = new Player();

                    if (int.TryParse(parts[0].Trim(), out var shirtNumber))
                        player.JerseyNumber = shirtNumber;

                    player.ExternalPlayerId = parts[1].Trim();
                    player.LastName = parts[2].Trim();

                    var birthRaw = parts[3].Trim();
                    if (DateTime.TryParseExact(
                            birthRaw,
                            "dd/MM/yyyy",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var birthDate))
                    {
                        player.BirthDate = new DateTimeOffset(birthDate);
                    }

                    if (int.TryParse(parts[4].Trim(), out var height))
                        player.HeightCm = height;

                    var roleFlags = parts.Length > 6 ? parts[6].Trim() : string.Empty;
                    if (!string.IsNullOrEmpty(roleFlags))
                    {
                        if (roleFlags.IndexOf('L', StringComparison.OrdinalIgnoreCase) >= 0)
                            player.PlayerRole = "L";
                        else if (roleFlags.IndexOf('C', StringComparison.OrdinalIgnoreCase) >= 0)
                            player.PlayerRole = "C";
                    }

                    if (parts.Length > 8)
                        player.FirstName = parts[8].Trim();

                    if (parts.Length > 9 && int.TryParse(parts[9].Trim(), out var positionValue))
                    {
                        if (Enum.IsDefined(typeof(PlayerPost), positionValue))
                        {
                            player.Position = (PlayerPost)positionValue;
                        }
                    }

                    if (parts.Length > 10)
                        player.NickName = parts[10].Trim();

                    if (parts.Length > 11)
                        player.IsForeign = ParseNullableBool(parts[11]);

                    if (parts.Length > 12)
                        player.TransferredOut = ParseNullableBool(parts[12]);

                    if (parts.Length > 13 && int.TryParse(parts[13].Trim(), out var birthSerial))
                        player.BirthDateSerial = birthSerial;

                    team.Players.Add(player);
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Výjimka při parsování: {ex.Message}";
                team = null!;
                return false;
            }
        }

        private List<string> BuildSqLines(Team team)
        {
            var lines = new List<string>();

            
            lines.Add("DV-Team-2\r");

            var teamFields = new List<string>
            {
                team.TeamCode ?? string.Empty,
                team.Name ?? string.Empty,
                team.CoachName ?? string.Empty,
                team.AssistantCoachName ?? string.Empty,
                team.Abbreviation ?? string.Empty,
                team.CharacterEncoding?.ToString() ?? string.Empty,
                team.NameHex ?? string.Empty,
                team.CoachNameHex ?? string.Empty,
                team.AssistantCoachNameHex ?? string.Empty,
                team.AbbreviationHex ?? string.Empty
            };

            lines.Add(string.Join('\t', teamFields));

            var players = team.Players ?? new List<Player>();
            foreach (var player in players.OrderBy(p => p.JerseyNumber))
            {
                var birthDateText = player.BirthDate.HasValue
                    ? player.BirthDate.Value.Date.ToString("dd/MM/yyyy")
                    : string.Empty;

                var positionNumber = ((int)player.Position).ToString();

                var lineFields = new List<string>
                {
                    player.JerseyNumber.ToString(CultureInfo.InvariantCulture),
                    player.ExternalPlayerId ?? string.Empty,
                    player.LastName ?? string.Empty,
                    birthDateText,
                    player.HeightCm?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    string.Empty, // column 5 unused
                    player.PlayerRole ?? string.Empty,
                    string.Empty, // column 7 unused
                    player.FirstName ?? string.Empty,
                    positionNumber,
                    player.NickName ?? string.Empty,
                    NullableBoolToString(player.IsForeign),
                    NullableBoolToString(player.TransferredOut),
                    player.BirthDateSerial?.ToString(CultureInfo.InvariantCulture) ?? string.Empty
                };

                lines.Add(string.Join('\t', lineFields));
            }

            return lines;
        }

        private bool? ParseNullableBool(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            raw = raw.Trim().ToLowerInvariant();

            return raw switch
            {
                "1" or "true" or "t" or "yes" or "y" => true,
                "0" or "false" or "f" or "no" or "n" => false,
                _ => null
            };
        }

        private string NullableBoolToString(bool? value)
        {
            if (!value.HasValue) return string.Empty;
            return value.Value ? "1" : "0";
        }

        private string[] ReadSqLines(string filePath)
        {
            // First try UTF-8; if replacement characters appear, fallback to CP1250 (common for Czech DV exports)
            var utf8 = File.ReadAllLines(filePath, Encoding.UTF8);
            if (!ContainsReplacementChar(utf8))
                return utf8;

            var cp1250 = File.ReadAllLines(filePath, Encoding.GetEncoding(1250));
            return cp1250;
        }

        private static bool ContainsReplacementChar(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                if (line.Contains('\uFFFD'))
                {
                    return true;
                }
            }
            return false;
        }

        public void SaveTeam(Team team)
        {
            using var db = CreateDb();
            db.Database.EnsureCreated();

            team.Players ??= new List<Player>();

            if (team.Id == 0)
            {
                db.Teams.Add(team);
                db.SaveChanges();

                foreach (var player in team.Players)
                {
                    player.Id = 0;
                    player.TeamId = team.Id;
                    db.Players.Add(player);
                }

                db.SaveChanges();
                return;
            }

            var existingTeam = db.Teams
                .Include(t => t.Players)
                .FirstOrDefault(t => t.Id == team.Id);

            if (existingTeam == null)
            {
                // pokud záznam neexistuje, ulož ho jako nový
                team.Id = 0;
                SaveTeam(team);
                return;
            }

            existingTeam.TeamCode = team.TeamCode;
            existingTeam.Name = team.Name;
            existingTeam.CoachName = team.CoachName;
            existingTeam.AssistantCoachName = team.AssistantCoachName;
            existingTeam.Abbreviation = team.Abbreviation;
            existingTeam.CharacterEncoding = team.CharacterEncoding;
            existingTeam.NameHex = team.NameHex;
            existingTeam.CoachNameHex = team.CoachNameHex;
            existingTeam.AssistantCoachNameHex = team.AssistantCoachNameHex;
            existingTeam.AbbreviationHex = team.AbbreviationHex;

            var incomingPlayers = team.Players;
            var incomingById = incomingPlayers
                .Where(p => p.Id != 0)
                .ToDictionary(p => p.Id);

            var toRemove = existingTeam.Players
                .Where(p => !incomingById.ContainsKey(p.Id))
                .ToList();

            foreach (var remove in toRemove)
            {
                db.Players.Remove(remove);
            }

            foreach (var existingPlayer in existingTeam.Players)
            {
                if (incomingById.TryGetValue(existingPlayer.Id, out var updated))
                {
                    existingPlayer.TeamId = existingTeam.Id;
                    existingPlayer.JerseyNumber = updated.JerseyNumber;
                    existingPlayer.PlayerRole = updated.PlayerRole;
                    existingPlayer.ExternalPlayerId = updated.ExternalPlayerId;
                    existingPlayer.LastName = updated.LastName;
                    existingPlayer.FirstName = updated.FirstName;
                    existingPlayer.BirthDate = updated.BirthDate;
                    existingPlayer.HeightCm = updated.HeightCm;
                    existingPlayer.Position = updated.Position;
                    existingPlayer.NickName = updated.NickName;
                    existingPlayer.IsForeign = updated.IsForeign;
                    existingPlayer.TransferredOut = updated.TransferredOut;
                    existingPlayer.BirthDateSerial = updated.BirthDateSerial;
                }
            }

            var newPlayers = incomingPlayers
                .Where(p => p.Id == 0 || !existingTeam.Players.Any(ep => ep.Id == p.Id))
                .ToList();
            foreach (var newPlayer in newPlayers)
            {
                newPlayer.Id = 0;
                newPlayer.TeamId = existingTeam.Id;
                db.Players.Add(newPlayer);
            }

            db.SaveChanges();
        }

        public List<Team> GetAllTeamsWithPlayers()
        {
            using var db = CreateDb();

            return db.Teams
                .Include(t => t.Players)
                .AsNoTracking()
                .ToList();
        }

        public void DeleteTeam(int teamId)
        {
            using var db = CreateDb();

            var team = db.Teams.FirstOrDefault(t => t.Id == teamId);
            if (team == null) return;

            db.Teams.Remove(team);
            db.SaveChanges();
        }
    }
}

