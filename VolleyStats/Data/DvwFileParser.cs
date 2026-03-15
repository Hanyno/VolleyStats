using Avalonia.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using VolleyStats.Models;
using VolleyStats.Enums;

namespace VolleyStats.Data
{
    public class DvwFileParser
    {
        /// <summary>
        /// Parses a .dvw file at the given path into a fully populated <see cref="Match"/> object.
        /// This is the main entry point � implement section dispatching here.
        /// </summary>
        public Match ParseDvwFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath, Encoding.GetEncoding("Windows-1250"));

            var match = new Match
            {
                Metadata = ParseMetadata(lines),
                Info = ParseInfo(lines),
                HomeTeam = ParseTeam(lines, isHome: true),
                AwayTeam = ParseTeam(lines, isHome: false),
                MoreInfo = ParseMoreInfo(lines),
                Comments = ParseComments(lines),
                Sets = ParseSets(lines),
                HomePlayers = ParsePlayers(lines, isHome: true),
                AwayPlayers = ParsePlayers(lines, isHome: false),
                AttackCombinations = ParseAttackCombinations(lines),
                SetterCalls = ParseSetterCalls(lines),
                WinningSymbols = ParseWinningSymbols(lines),
                Reserve = ParseReserve(lines),
                VideoPaths = ParseVideoPaths(lines),
                ScoutCodes = ParseScoutCodes(lines),
            };

            return match;
        }

        private MatchMetadata ParseMetadata(string[] lines)
        {
            var result = new MatchMetadata();
            bool inSection = false;   

            foreach (string rawLine in lines)
            {
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.Equals("[3DATAVOLLEYSCOUT]", StringComparison.OrdinalIgnoreCase))
                {
                    inSection = true;
                    continue;
                }

                if (inSection && line.StartsWith("[") && line.EndsWith("]"))
                    break;

                if (!inSection)
                    continue;

                var parts = line.Split(new[] { ':' }, 2);

                if (parts.Length != 2)
                    continue;

                var key = parts[0].Trim().ToUpper();
                var value = parts[1].Trim();

                switch (key)
                {
                    case "FILEFORMAT": result.FileFormat = value; break;

                    case "GENERATOR-DAY": result.GenDay = value; break;
                    case "GENERATOR-IDP": result.GenIdp = value; break;
                    case "GENERATOR-PRG": result.GenPrg = value; break;
                    case "GENERATOR-REL": result.GenRel = value; break;
                    case "GENERATOR-VER": result.GenVer = value; break;
                    case "GENERATOR-NAM": result.GenName = value; break;

                    case "LASTCHANGE-DAY": result.LastDay = value; break;
                    case "LASTCHANGE-IDP": result.LastIdp = value; break;
                    case "LASTCHANGE-PRG": result.LastPrg = value; break;
                    case "LASTCHANGE-REL": result.LastRel = value; break;
                    case "LASTCHANGE-VER": result.LastVer = value; break;
                    case "LASTCHANGE-NAM": result.LastName = value; break;
                }
            }
            return result;
        }

        private MatchInfo ParseInfo(string[] lines)
        {
            if (lines is null) throw new ArgumentNullException(nameof(lines));

            int sectionIndex = Array.FindIndex(lines, l =>
                l != null && l.Trim().Equals("[3MATCH]", StringComparison.OrdinalIgnoreCase));

            if (sectionIndex < 0)
                throw new InvalidOperationException("Section [3MATCH] not found.");

            string? first = null;
            string? second = null;

            for (int i = sectionIndex + 1; i < lines.Length; i++)
            {
                var line = (lines[i] ?? "").Trim();
                if (line.Length == 0) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                    break;

                if (first == null) first = line;
                else { second = line; break; }
            }

            if (first == null)
                throw new InvalidOperationException("Section [3MATCH] found but contains no data line.");

            var info = new MatchInfo();

            var f1 = SplitSemicolonKeepEmpties(first);

            info.Date = ParseDateOnly(f1, 0);
            info.Time = ParseTimeOnly(f1, 1);
            info.Seasson = Get(f1, 2) ?? "";
            info.League = Get(f1, 3) ?? "";
            info.Phase = Get(f1, 4) ?? "";

            info.Day = ParseInt(f1, 6);
            info.Number = Get(f1, 7) ?? "";
            info.CharEncoding = ParseInt(f1, 8);

            if (!string.IsNullOrWhiteSpace(second))
            {
                var f2 = SplitSemicolonKeepEmpties(second);

                info.idk1 = NullIfEmpty(Get(f2, 0));
                info.idk2 = NullIfEmpty(Get(f2, 1));
                info.WeirdDate = ParseInt(f2, 2);

                info.idk3 = NullIfEmpty(Get(f2, 7));
                info.idk4 = NullIfEmpty(Get(f2, 8));
                info.EndTime = NullIfEmpty(Get(f2, 10));
            }

            return info;
        }


        private MatchTeam ParseTeam(string[] lines, bool isHome)
        {
            if (lines is null) throw new ArgumentNullException(nameof(lines));

            int sectionIndex = Array.FindIndex(lines, l =>
                l != null && l.Trim().Equals("[3TEAMS]", StringComparison.OrdinalIgnoreCase));

            if (sectionIndex < 0)
                throw new InvalidOperationException("Section [3TEAMS] not found.");

            string? homeLine = null;
            string? awayLine = null;

            for (int i = sectionIndex + 1; i < lines.Length; i++)
            {
                var line = (lines[i] ?? "").Trim();
                if (line.Length == 0) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                    break;

                if (homeLine == null) homeLine = line;
                else { awayLine = line; break; }
            }

            var selected = isHome ? homeLine : awayLine;

            if (selected == null)
                throw new InvalidOperationException($"Section [3TEAMS] does not contain {(isHome ? "home" : "away")} team line.");

            var f = SplitSemicolonKeepEmpties(selected);

            var team = new MatchTeam
            {
                TeamCode = (Get(f, 0) ?? "").Trim(),
                Name = (Get(f, 1) ?? "").Trim(),
                SetsWon = ParseInt(f, 2, 0),
                CoachName = NullIfEmpty(Get(f, 3)),
                AssistantCoachName = NullIfEmpty(Get(f, 4)),
                Color = ParseColorToHexOrRaw(Get(f, 5))
            };

            return team;
        }

        private MatchMoreInfo ParseMoreInfo(string[] lines)
        {
            if (lines is null)
                throw new ArgumentNullException(nameof(lines));

            int sectionIndex = Array.FindIndex(lines, l =>
                l != null && l.Trim().Equals("[3MORE]", StringComparison.OrdinalIgnoreCase));

            if (sectionIndex < 0)
                throw new InvalidOperationException("Section [3MORE] not found.");

            string? dataLine = null;

            for (int i = sectionIndex + 1; i < lines.Length; i++)
            {
                var line = (lines[i] ?? "").Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                    break;

                dataLine = line;
                break;
            }

            if (dataLine == null)
                throw new InvalidOperationException("[3MORE] section contains no data.");

            var f = SplitSemicolonKeepEmpties(dataLine);

            return new MatchMoreInfo
            {
                Referees = NullIfEmpty(Get(f, 0)),
                Spectators = ParseNullableInt(Get(f, 1)),
                Receipts = ParseNullableInt(Get(f, 2)),
                City = NullIfEmpty(Get(f, 3)),
                Hall = NullIfEmpty(Get(f, 4)),
                Scout = NullIfEmpty(Get(f, 5))
            };
        }

        private MatchComments ParseComments(string[] lines)
        {
            if (lines is null) throw new ArgumentNullException(nameof(lines));

            int sectionIndex = Array.FindIndex(lines, l =>
                l != null && l.Trim().Equals("[3COMMENTS]", StringComparison.OrdinalIgnoreCase));

            if (sectionIndex < 0)
                return new MatchComments();

            string? dataLine = null;

            for (int i = sectionIndex + 1; i < lines.Length; i++)
            {
                var line = (lines[i] ?? "").Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                    break;

                dataLine = line;
                break;
            }

            if (string.IsNullOrWhiteSpace(dataLine))
                return new MatchComments();

            if (!dataLine.Contains(';'))
            {
                return new MatchComments
                {
                    CommentSummarry = dataLine.Trim()
                };
            }

            var parts = SplitSemicolonKeepEmpties(dataLine);

            return new MatchComments
            {
                CommentSummarry = (Get(parts, 0)?.Trim() is { Length: > 0 } s) ? s : "No comment",
                MatchDesc = NullIfEmpty(Get(parts, 1)),
                HomeCoachComment = NullIfEmpty(Get(parts, 2)),
                AwayCoachComment = NullIfEmpty(Get(parts, 3))
            };
        }

        private List<MatchSet> ParseSets(string[] lines)
        {
            if (lines is null) throw new ArgumentNullException(nameof(lines));

            int sectionIndex = Array.FindIndex(lines, l =>
                l != null && l.Trim().Equals("[3SET]", StringComparison.OrdinalIgnoreCase));

            if (sectionIndex < 0)
                return new List<MatchSet>();

            var result = new List<MatchSet>();

            for (int i = sectionIndex + 1; i < lines.Length; i++)
            {
                var line = (lines[i] ?? "").Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                    break;

                var fields = SplitSemicolonKeepEmpties(line);

                if (fields.Length < 3)
                    continue;

                bool idk = bool.TryParse(fields[0].Trim(), out var b) && b;

                int duration = ParseInt(fields[^1]);

                var finalScore = TryParseScore(fields[^2]);

                var partials = new List<MatchScore>();
                for (int j = 1; j <= fields.Length - 3; j++)
                {
                    var ps = TryParseScore(fields[j]);
                    if (ps != null) partials.Add(ps);
                }

                result.Add(new MatchSet
                {
                    Idk = idk,
                    Duration = duration,
                    FinalScore = finalScore,
                    PartialScores = partials.Count > 0 ? partials : null
                });
            }

            return result;
        }

        private List<MatchPlayer> ParsePlayers(string[] lines, bool isHome)
        {
            if (lines is null) throw new ArgumentNullException(nameof(lines));

            string sectionName = isHome ? "[3PLAYERS-H]" : "[3PLAYERS-V]";

            int sectionIndex = Array.FindIndex(lines, l =>
                l != null && l.Trim().Equals(sectionName, StringComparison.OrdinalIgnoreCase));

            if (sectionIndex < 0)
                return new List<MatchPlayer>();

            var players = new List<MatchPlayer>();

            for (int i = sectionIndex + 1; i < lines.Length; i++)
            {
                var line = (lines[i] ?? "").Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                    break;

                var f = SplitSemicolonKeepEmpties(line);

                if (f.Length < 14)
                    continue;

                var p = new MatchPlayer
                {
                    IsHome = isHome,
                    JerseyNumber = ParseInt(Get(f, 1)),
                    RandomId = ParseInt(Get(f, 2)),
                    StartingZones = ParseStartingZones(f, startIndex: 3, count: 5),
                    ExternalPlayerId = (Get(f, 8) ?? "").Trim(),
                    LastName = NullIfEmpty(Get(f, 9)),
                    FirstName = NullIfEmpty(Get(f, 10)),
                    BirthDate = ParseNullableDateOnly(Get(f, 11)),
                    PlayerRole = NullIfEmpty(Get(f, 12)),
                    Position = ParsePosition(Get(f, 13), roleHint: Get(f, 12)),
                    IsForeign = ParseNullableBool(Get(f, 14)),
                };

                players.Add(p);
            }

            return players;
        }


        private List<AttackCombination> ParseAttackCombinations(string[] lines)
        {
            if (lines is null) throw new ArgumentNullException(nameof(lines));

            const string sectionName = "[3ATTACKCOMBINATION]";

            int sectionIndex = Array.FindIndex(lines, l =>
                l != null && l.Trim().Equals(sectionName, StringComparison.OrdinalIgnoreCase));

            if (sectionIndex < 0)
                return new List<AttackCombination>();

            var result = new List<AttackCombination>();

            for (int i = sectionIndex + 1; i < lines.Length; i++)
            {
                var line = (lines[i] ?? "").Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                    break;

                var f = SplitSemicolonKeepEmpties(line);

                if (f.Length < 9)
                    continue;

                var combo = new AttackCombination
                {
                    Combination = (Get(f, 0) ?? "").Trim(),
                    StartPoint = ParseInt(Get(f, 1)),
                    Dir = ParseChar(Get(f, 2)),
                    Type = ParseChar(Get(f, 3)),
                    Desc = NullIfEmpty(Get(f, 4)),
                    IdkRandom = NullIfEmpty(Get(f, 5)),
                    Color = ParseColorToHexOrRaw(Get(f, 6)),
                    SetterCall = ParseChar(Get(f, 8)),
                    IdkRandom2 = ParseBoolish(Get(f, 9))
                };

                if (string.IsNullOrWhiteSpace(combo.Combination))
                    continue;

                result.Add(combo);
            }

            return result;
        }


        private List<SetterCall> ParseSetterCalls(string[] lines)
        {
            if (lines is null) throw new ArgumentNullException(nameof(lines));

            const string sectionName = "[3SETTERCALL]";

            int sectionIndex = Array.FindIndex(lines, l =>
                l != null && l.Trim().Equals(sectionName, StringComparison.OrdinalIgnoreCase));

            if (sectionIndex < 0)
                return new List<SetterCall>();

            var result = new List<SetterCall>();

            for (int i = sectionIndex + 1; i < lines.Length; i++)
            {
                var line = (lines[i] ?? "").Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                    break;

                var f = SplitSemicolonKeepEmpties(line);

                // call;idk;name;desc;arrColor; c1; c2; c3; polygon; color;
                if (f.Length < 10)
                    continue;

                var call = new SetterCall
                {
                    Call = (Get(f, 0) ?? "").Trim(),
                    idk = (Get(f, 1) ?? "").Trim(),
                    Name = (Get(f, 2) ?? "").Trim(),
                    Desc = (Get(f, 3) ?? "").Trim(),
                    ArrColor = ParseColorToHexOrRaw(Get(f, 4)),
                    ArrCoordinates = ParseNonEmptyList(new[] { Get(f, 5), Get(f, 6), Get(f, 7) }),
                    PolygonCoordinates = ParseCsvList(Get(f, 8)),
                    Color = ParseColorToHexOrRaw(Get(f, 9))
                };

                if (!string.IsNullOrWhiteSpace(call.Call))
                    result.Add(call);
            }

            return result;
        }


        private WinningSymbols ParseWinningSymbols(string[] lines)
        {
            if (lines is null) throw new ArgumentNullException(nameof(lines));

            const string sectionName = "[3WINNINGSYMBOLS]";

            int sectionIndex = Array.FindIndex(lines, l =>
                l != null && l.Trim().Equals(sectionName, StringComparison.OrdinalIgnoreCase));

            if (sectionIndex < 0)
                return new WinningSymbols { Symbols = string.Empty };

            for (int i = sectionIndex + 1; i < lines.Length; i++)
            {
                var line = (lines[i] ?? "").Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                    break;

                return new WinningSymbols
                {
                    Symbols = line
                };
            }

            return new WinningSymbols { Symbols = string.Empty };
        }

        private string ParseReserve(string[] lines)
        {
            if (lines is null) throw new ArgumentNullException(nameof(lines));

            const string sectionName = "[3RESERVE]";

            int sectionIndex = Array.FindIndex(lines, l =>
                l != null && l.Trim().Equals(sectionName, StringComparison.OrdinalIgnoreCase));

            if (sectionIndex < 0)
                return string.Empty;

            for (int i = sectionIndex + 1; i < lines.Length; i++)
            {
                var line = (lines[i] ?? "").Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                    return string.Empty;

                return line; // kdyby tam n�hodou n�co bylo
            }

            return string.Empty;
        }

        private List<string> ParseVideoPaths(string[] lines)
        {
            if (lines is null) throw new ArgumentNullException(nameof(lines));

            const string sectionName = "[3VIDEO]";

            int sectionIndex = Array.FindIndex(lines, l =>
                l != null && l.Trim().Equals(sectionName, StringComparison.OrdinalIgnoreCase));

            if (sectionIndex < 0)
                return new List<string>();

            var result = new List<string>();

            for (int i = sectionIndex + 1; i < lines.Length; i++)
            {
                var line = (lines[i] ?? "").Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                    break;

                int eq = line.IndexOf('=');
                if (eq < 0 || eq == line.Length - 1)
                    continue;

                var path = line.Substring(eq + 1).Trim();
                if (path.Length > 0)
                    result.Add(path);
            }

            return result;
        }

        private List<Code> ParseScoutCodes(string[] lines)
        {
            if (lines is null) throw new ArgumentNullException(nameof(lines));

            const string sectionName = "[3SCOUT]";

            int sectionIndex = Array.FindIndex(lines, l =>
                l != null && l.Trim().Equals(sectionName, StringComparison.OrdinalIgnoreCase));

            if (sectionIndex < 0)
                return new List<Code>();

            var rawLines = new List<string>();

            for (int i = sectionIndex + 1; i < lines.Length; i++)
            {
                var line = (lines[i] ?? "").Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                    break;

                rawLines.Add(line);
            }

            return CodeClassifier.ParseLines(rawLines).ToList();
        }

        private static string? Get(string[] arr, int index)
    => (index >= 0 && index < arr.Length) ? arr[index] : null;

        private static string? NullIfEmpty(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            return s.Trim();
        }

        private static int ParseInt(string[] arr, int index, int fallback = 0)
        {
            var s = Get(arr, index);
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;
        }

        private static DateOnly ParseDateOnly(string[] arr, int index)
        {
            var s = (Get(arr, index) ?? "").Trim();

            string[] formats = { "MM/dd/yyyy", "dd/MM/yyyy", "M/d/yyyy", "d/M/yyyy" };

            if (DateOnly.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;

            throw new FormatException($"Invalid date in [3MATCH]: '{s}'");
        }

        private static TimeOnly ParseTimeOnly(string[] arr, int index)
        {
            var s = (Get(arr, index) ?? "").Trim();

            string[] formats = { "HH.mm.ss", "H.mm.ss", "HH:mm:ss", "H:mm:ss" };

            if (TimeOnly.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
                return t;

            throw new FormatException($"Invalid time in [3MATCH]: '{s}'");
        }

        private static string[] SplitSemicolonKeepEmpties(string line)
        {
            var parts = line.Split(';');
            if (parts.Length > 0 && parts[^1] == "")
                Array.Resize(ref parts, parts.Length - 1);
            return parts;
        }
        private static string ParseColorToHexOrRaw(string? colorField)
        {
            var raw = (colorField ?? "").Trim();
            if (raw.Length == 0) return "#000000";

            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                return raw;

            int rgb = n & 0x00FFFFFF;
            return $"#{rgb:X6}";
        }
        private static int? ParseNullableInt(string? s)
        {
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                return v;
            return null;
        }
        private static int ParseInt(string? s, int fallback = 0)
        {
            if (int.TryParse((s ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                return v;
            return fallback;
        }
        private static MatchScore? TryParseScore(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;

            var t = s.Trim().Replace(" ", "");

            var dashIndex = t.IndexOf('-');
            if (dashIndex <= 0 || dashIndex >= t.Length - 1)
                return null;

            var left = t.Substring(0, dashIndex);
            var right = t.Substring(dashIndex + 1);

            if (!int.TryParse(left, NumberStyles.Integer, CultureInfo.InvariantCulture, out var home))
                return null;
            if (!int.TryParse(right, NumberStyles.Integer, CultureInfo.InvariantCulture, out var away))
                return null;

            return new MatchScore { HomeTeamScore = home, AwayTeamScore = away };
        }
        private static List<string> ParseStartingZones(string[] f, int startIndex, int count)
        {
            var list = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                var v = (Get(f, startIndex + i) ?? "").Trim();
                if (v.Length > 0)
                    list.Add(v);
            }
            return list;
        }
        private static PlayerPost ParsePosition(string? s, string? roleHint)
        {
            if (int.TryParse((s ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) &&
                Enum.IsDefined(typeof(PlayerPost), v))
                return (PlayerPost)v;

            if (string.Equals((roleHint ?? "").Trim(), "L", StringComparison.OrdinalIgnoreCase))
                return PlayerPost.Libero;

            return PlayerPost.None;
        }
        private static DateOnly? ParseNullableDateOnly(string? s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return null;

            string[] formats = { "dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "M/d/yyyy", "yyyy-MM-dd" };

            if (DateOnly.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;

            return null;
        }

        private static bool? ParseNullableBool(string? s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return null;

            if (bool.TryParse(s, out var b)) return b;
            if (s == "1") return true;
            if (s == "0") return false;

            return null;
        }
        private static bool ParseBoolish(string? s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return false;

            if (bool.TryParse(s, out var b)) return b;
            if (s == "1") return true;
            if (s == "0") return false;

            if (s.Equals("Y", StringComparison.OrdinalIgnoreCase)) return true;
            if (s.Equals("N", StringComparison.OrdinalIgnoreCase)) return false;

            return false;
        }
        private static char ParseChar(string? s, char fallback = '\0')
        {
            s = (s ?? "").Trim();
            return s.Length > 0 ? s[0] : fallback;
        }

        private static List<string> ParseCsvList(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new List<string>();
            return s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .ToList();
        }
        private static List<string> ParseNonEmptyList(IEnumerable<string?> items)
        {
            return items.Select(x => (x ?? "").Trim())
                        .Where(x => x.Length > 0)
                        .ToList();
        }

        /// <summary>
        /// Parses only the summary information (date, time, season, league, phase, teams, sets)
        /// from a .dvw file using the same section-based approach as <see cref="ParseDvwFile"/>.
        /// </summary>
        public MatchSummary ParseMatchSummary(string filePath)
        {
            var summary = new MatchSummary
            {
                FileName = System.IO.Path.GetFileNameWithoutExtension(filePath)
            };

            string[] lines;
            try
            {
                lines = File.ReadAllLines(filePath, Encoding.GetEncoding("Windows-1250"));
                if (lines.Length == 0)
                    return summary;
            }
            catch
            {
                return summary;
            }

            TryFillInfoFromMatch(lines, summary);
            TryFillTeamsFromSection(lines, summary);

            if (!summary.Date.HasValue)
            {
                var created = File.GetCreationTime(filePath);
                summary.Date = DateOnly.FromDateTime(created);
                summary.Time ??= TimeOnly.FromDateTime(created);
            }

            return summary;
        }

        private void TryFillInfoFromMatch(string[] lines, MatchSummary summary)
        {
            int sectionIndex = Array.FindIndex(lines, l =>
                l != null && l.Trim().Equals("[3MATCH]", StringComparison.OrdinalIgnoreCase));

            if (sectionIndex < 0)
                return;

            for (int i = sectionIndex + 1; i < lines.Length; i++)
            {
                var line = (lines[i] ?? "").Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("[") && line.EndsWith("]")) break;

                var f = SplitSemicolonKeepEmpties(line);

                var dateStr = (Get(f, 0) ?? "").Trim();
                string[] dateFormats = { "MM/dd/yyyy", "dd/MM/yyyy", "M/d/yyyy", "d/M/yyyy" };
                if (DateOnly.TryParseExact(dateStr, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    summary.Date = date;

                var timeStr = (Get(f, 1) ?? "").Trim();
                string[] timeFormats = { "HH.mm.ss", "H.mm.ss", "HH:mm:ss", "H:mm:ss" };
                if (TimeOnly.TryParseExact(timeStr, timeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                    summary.Time = time;

                summary.Season = NullIfEmpty(Get(f, 2));
                summary.League = NullIfEmpty(Get(f, 3));
                summary.Phase = NullIfEmpty(Get(f, 4));

                break;
            }
        }

        private void TryFillTeamsFromSection(string[] lines, MatchSummary summary)
        {
            int sectionIndex = Array.FindIndex(lines, l =>
                l != null && l.Trim().Equals("[3TEAMS]", StringComparison.OrdinalIgnoreCase));

            if (sectionIndex < 0)
                return;

            string? homeLine = null;
            string? awayLine = null;

            for (int i = sectionIndex + 1; i < lines.Length; i++)
            {
                var line = (lines[i] ?? "").Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("[") && line.EndsWith("]")) break;

                if (homeLine == null) homeLine = line;
                else { awayLine = line; break; }
            }

            if (homeLine != null)
            {
                var f = SplitSemicolonKeepEmpties(homeLine);
                summary.HomeTeam = (Get(f, 1) ?? "").Trim();
                summary.HomeSets = ParseNullableInt(Get(f, 2));
            }

            if (awayLine != null)
            {
                var f = SplitSemicolonKeepEmpties(awayLine);
                summary.AwayTeam = (Get(f, 1) ?? "").Trim();
                summary.AwaySets = ParseNullableInt(Get(f, 2));
            }
        }

    }
}
