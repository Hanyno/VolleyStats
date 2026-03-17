using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VolleyStats.Models;

namespace VolleyStats.Data
{
    public class MatchSummaryLoader
    {
        public IReadOnlyList<string> GetAvailableSeasons()
        {
            var seasons = new List<string>();
            foreach (var root in GetSeasonRoots())
            {
                if (!Directory.Exists(root))
                    continue;

                var dirs = Directory.EnumerateDirectories(root)
                    .Select(Path.GetFileName)
                    .Where(n => !string.IsNullOrWhiteSpace(n));

                seasons.AddRange(dirs!);
            }

            return seasons
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(x => x)
                .ToList();
        }

        public async Task<IReadOnlyList<MatchSummary>> LoadMatchesAsync(string? season, string? teamFilter, IReadOnlyCollection<Team>? knownTeams = null, bool sortDescending = true, CancellationToken cancellationToken = default)
        {
            var seasonPath = ResolveSeasonPath(season, out var resolvedSeasonName);
            if (string.IsNullOrWhiteSpace(seasonPath))
                return Array.Empty<MatchSummary>();

            var matchesDir = Path.Combine(seasonPath, "Matches");
            if (!Directory.Exists(matchesDir))
                return Array.Empty<MatchSummary>();

            var result = new List<MatchSummary>();
            var files = Directory.EnumerateFiles(matchesDir, "*.dvw", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var summary = await Task.Run(() => ParseFile(file, knownTeams), cancellationToken);
                summary.Season ??= resolvedSeasonName;

                if (!string.IsNullOrWhiteSpace(teamFilter) && !MatchesTeam(summary, teamFilter))
                    continue;

                result.Add(summary);
            }

            if (sortDescending)
                return result
                    .OrderByDescending(m => m.Date ?? DateOnly.MinValue)
                    .ThenByDescending(m => m.Time ?? TimeOnly.MinValue)
                    .ThenBy(m => m.FileName)
                    .ToList();

            return result
                .OrderBy(m => m.Date ?? DateOnly.MaxValue)
                .ThenBy(m => m.Time ?? TimeOnly.MaxValue)
                .ThenBy(m => m.FileName)
                .ToList();
        }

        private static string? ResolveSeasonPath(string? season, out string? resolvedSeasonName)
        {
            foreach (var root in GetSeasonRoots())
            {
                if (!Directory.Exists(root))
                    continue;

                if (!string.IsNullOrWhiteSpace(season))
                {
                    var seasonDir = Path.Combine(root, season);
                    if (Directory.Exists(seasonDir))
                    {
                        resolvedSeasonName = Path.GetFileName(seasonDir);
                        return seasonDir;
                    }
                }

                var fallback = Directory.EnumerateDirectories(root).FirstOrDefault();
                if (fallback != null)
                {
                    resolvedSeasonName = Path.GetFileName(fallback);
                    return fallback;
                }
            }

            resolvedSeasonName = season;
            return null;
        }

        private static IEnumerable<string> GetSeasonRoots()
        {
            var baseDir = AppContext.BaseDirectory;
            yield return Path.Combine(baseDir, "Seasons");
        }

        private static bool MatchesTeam(MatchSummary summary, string teamFilter)
        {
            if (string.IsNullOrWhiteSpace(teamFilter))
                return true;

            return summary.HomeTeam.Contains(teamFilter, StringComparison.OrdinalIgnoreCase)
                || summary.AwayTeam.Contains(teamFilter, StringComparison.OrdinalIgnoreCase);
        }

        private static MatchSummary ParseFile(string filePath, IReadOnlyCollection<Team>? knownTeams)
        {
            var parser = new DvwFileParser();
            var summary = parser.ParseMatchSummary(filePath);
            summary.FilePath = filePath;

            if (string.IsNullOrWhiteSpace(summary.HomeTeam) && string.IsNullOrWhiteSpace(summary.AwayTeam))
            {
                TryParseTeamsFromFileName(summary, knownTeams);
            }

            return summary;
        }

        private static void TryParseHeader(IReadOnlyList<string> lines, MatchSummary summary)
        {
            var headerLine = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("["));
            if (headerLine == null)
                return;

            var parts = SplitLine(headerLine);
            if (parts.Count >= 1 && TryParseDate(parts[0], out var date))
                summary.Date = date;

            if (parts.Count >= 2 && TryParseTime(parts[1], out var time))
                summary.Time = time;

            if (parts.Count >= 3 && string.IsNullOrWhiteSpace(summary.Season))
                summary.Season = parts[2];

            if (parts.Count >= 4)
                summary.League = parts[3];

            if (parts.Count >= 5)
                summary.Phase = parts[4];
        }

        private static void TryParseTeams(IReadOnlyList<string> lines, MatchSummary summary, IReadOnlyCollection<Team>? knownTeams)
        {
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                    continue;

                var parts = SplitLine(line);
                if (parts.Count < 2)
                    continue;

                if (IsHomeMarker(parts[0]) && parts.Count >= 3)
                {
                    summary.HomeTeam = string.IsNullOrWhiteSpace(parts[2]) ? parts[1] : parts[2];
                    summary.HomeSets ??= TryParseNullableInt(parts.Last());
                }
                else if (IsAwayMarker(parts[0]) && parts.Count >= 3)
                {
                    summary.AwayTeam = string.IsNullOrWhiteSpace(parts[2]) ? parts[1] : parts[2];
                    summary.AwaySets ??= TryParseNullableInt(parts.Last());
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(summary.HomeTeam))
                    {
                        var homeCandidate = TryMatchKnownTeam(parts, knownTeams);
                        if (!string.IsNullOrWhiteSpace(homeCandidate))
                        {
                            summary.HomeTeam = homeCandidate;
                            summary.HomeSets ??= FindTrailingScore(parts);
                            continue;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(summary.AwayTeam))
                    {
                        var awayCandidate = TryMatchKnownTeam(parts, knownTeams);
                        if (!string.IsNullOrWhiteSpace(awayCandidate) && !string.Equals(awayCandidate, summary.HomeTeam, StringComparison.OrdinalIgnoreCase))
                        {
                            summary.AwayTeam = awayCandidate;
                            summary.AwaySets ??= FindTrailingScore(parts);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(summary.HomeTeam) && !string.IsNullOrWhiteSpace(summary.AwayTeam))
                    break;
            }
        }

        private static void TryParseSets(IReadOnlyList<string> lines, MatchSummary summary)
        {
            if (summary.HomeSets.HasValue && summary.AwaySets.HasValue)
                return;

            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"(?<home>\d+)\s*-\s*(?<away>\d+)");
                if (match.Success)
                {
                    var home = TryParseNullableInt(match.Groups["home"].Value);
                    var away = TryParseNullableInt(match.Groups["away"].Value);

                    if (!summary.HomeSets.HasValue)
                        summary.HomeSets = home;

                    if (!summary.AwaySets.HasValue)
                        summary.AwaySets = away;

                    if (summary.HomeSets.HasValue && summary.AwaySets.HasValue)
                        return;
                }
            }
        }

        private static void TryParseTeamsFromFileName(MatchSummary summary, IReadOnlyCollection<Team>? knownTeams)
        {
            var name = summary.FileName.Trim('&');
            var separators = new[] { " vs ", " VS ", " - ", "_", " v " };
            foreach (var sep in separators)
            {
                var split = name.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length == 2)
                {
                    summary.HomeTeam = split[0].Trim();
                    summary.AwayTeam = split[1].Trim();
                    return;
                }
            }

            if (knownTeams != null)
            {
                foreach (var team in knownTeams)
                {
                    if (name.Contains(team.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrWhiteSpace(summary.HomeTeam))
                            summary.HomeTeam = team.Name;
                        else if (string.IsNullOrWhiteSpace(summary.AwayTeam) && !summary.HomeTeam.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
                            summary.AwayTeam = team.Name;
                    }
                }
            }
        }

        private static int? FindTrailingScore(IReadOnlyList<string> parts)
        {
            for (var i = parts.Count - 1; i >= 0; i--)
            {
                var number = TryParseNullableInt(parts[i]);
                if (number.HasValue)
                    return number;
            }

            return null;
        }

        private static bool IsHomeMarker(string value)
        {
            return value.Equals("H", StringComparison.OrdinalIgnoreCase)
                || value.Equals("HOME", StringComparison.OrdinalIgnoreCase)
                || value.Equals("HOME_TEAM", StringComparison.OrdinalIgnoreCase)
                || value.Equals("1");
        }

        private static bool IsAwayMarker(string value)
        {
            return value.Equals("V", StringComparison.OrdinalIgnoreCase)
                || value.Equals("G", StringComparison.OrdinalIgnoreCase)
                || value.Equals("GUEST", StringComparison.OrdinalIgnoreCase)
                || value.Equals("AWAY", StringComparison.OrdinalIgnoreCase)
                || value.Equals("2");
        }

        private static string? TryMatchKnownTeam(IReadOnlyList<string> parts, IReadOnlyCollection<Team>? knownTeams)
        {
            if (knownTeams == null)
                return null;

            foreach (var part in parts)
            {
                foreach (var team in knownTeams)
                {
                    if (part.Contains(team.Name, StringComparison.OrdinalIgnoreCase))
                        return team.Name;

                    if (!string.IsNullOrWhiteSpace(team.Abbreviation) && part.Contains(team.Abbreviation, StringComparison.OrdinalIgnoreCase))
                        return team.Name;
                }
            }

            return null;
        }

        private static List<string> SplitLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return new List<string>();

            if (line.Contains('\t'))
                return line.Split('\t').Select(p => p.Trim()).ToList();

            if (line.Contains(';'))
                return line.Split(';').Select(p => p.Trim()).ToList();

            if (line.Contains(','))
                return line.Split(',').Select(p => p.Trim()).ToList();

            return line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();
        }

        private static bool TryParseDate(string? value, out DateOnly date)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (DateOnly.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
                    return true;

                if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    return true;

                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    date = DateOnly.FromDateTime(dt);
                    return true;
                }
            }

            date = default;
            return false;
        }

        private static bool TryParseTime(string? value, out TimeOnly time)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (TimeOnly.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out time))
                    return true;

                if (TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
                    return true;

                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    time = TimeOnly.FromDateTime(dt);
                    return true;
                }
            }

            time = default;
            return false;
        }

        private static int? TryParseNullableInt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                return number;

            return null;
        }
    }
}
