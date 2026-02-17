using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VolleyStats.Domain
{
    /// <summary>
    /// Základní reprezentace kódu akce z DataVolley (.sq) řádku.
    /// Odvozené třídy (servis, útok, nahrávka, atd.) mohou stavět nad touto parsovanou kostrou.
    /// </summary>
    public abstract class Code
    {
        public string RawLine { get; }
        public string RawCode { get; }
        public char SkillLetter { get; }
        public char Sp { get; }
        public char Pr { get; }

        public CourtCoordinate? Start { get; }
        public CourtCoordinate? Middle { get; }
        public CourtCoordinate? End { get; }

        public DateTime? RecordedAt { get; }
        public int? SetNumber { get; }
        public int? HomeSetterZone { get; }
        public int? AwaySetterZone { get; }
        public int? VideoFile { get; }
        public int? VideoSecond { get; }
        public int[] HomeZones { get; } = Array.Empty<int>();
        public int[] AwayZones { get; } = Array.Empty<int>();

        protected Code(string rawLine)
        {
            RawLine = rawLine ?? string.Empty;

            var parts = SplitParts(rawLine);

            RawCode = parts.ElementAtOrDefault(0) ?? string.Empty;
            SkillLetter = string.IsNullOrEmpty(RawCode) ? '\0' : RawCode[0];

            Sp = FirstCharOrDefault(parts.ElementAtOrDefault(1));
            Pr = FirstCharOrDefault(parts.ElementAtOrDefault(2));

            Start = ParseCoordinate(parts.ElementAtOrDefault(4));
            Middle = ParseCoordinate(parts.ElementAtOrDefault(5));
            End = ParseCoordinate(parts.ElementAtOrDefault(6));

            RecordedAt = ParseTime(parts.ElementAtOrDefault(7));
            SetNumber = ParseNullableInt(parts.ElementAtOrDefault(8));
            HomeSetterZone = ParseNullableInt(parts.ElementAtOrDefault(9));
            AwaySetterZone = ParseNullableInt(parts.ElementAtOrDefault(10));

            VideoFile = ParseNullableInt(parts.ElementAtOrDefault(11));
            VideoSecond = ParseNullableInt(parts.ElementAtOrDefault(12));

            HomeZones = ParseZones(parts, 14);
            AwayZones = ParseZones(parts, 20);
        }

        private static string[] SplitParts(string rawLine)
        {
            return string.IsNullOrEmpty(rawLine)
                ? Array.Empty<string>()
                : rawLine.Split(';');
        }

        private static char FirstCharOrDefault(string? value)
        {
            return string.IsNullOrEmpty(value) ? '\0' : value[0];
        }

        private static CourtCoordinate? ParseCoordinate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();

            // Primární formát DV: XXYY (dvě cifry X + dvě cifry Y), např. "3456". Hodnoty jsou kladné; chybějící koordináty bývají "-1-1".
            if (trimmed.Length == 4 && trimmed.All(char.IsDigit))
            {
                if (int.TryParse(trimmed.Substring(0, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)
                    && int.TryParse(trimmed.Substring(2, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
                {
                    return new CourtCoordinate(x, y);
                }
            }

            // Fallback pro starší formát "x-y" nebo "x,y" a sentinel "-1-1".
            var normalized = trimmed.Replace(",", "-");
            var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2
                && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var xDash)
                && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var yDash))
            {
                return new CourtCoordinate(xDash, yDash);
            }

            return null;
        }

        private static DateTime? ParseTime(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;

            return null;
        }

        private static int? ParseNullableInt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                return i;

            return null;
        }

        private static int[] ParseZones(IReadOnlyList<string> parts, int startIndex)
        {
            var zones = new List<int>(6);
            for (int i = 0; i < 6; i++)
            {
                var value = parts.ElementAtOrDefault(startIndex + i);
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var zone))
                {
                    zones.Add(zone);
                }
            }

            return zones.ToArray();
        }
    }

    public readonly record struct CourtCoordinate(int X, int Y);
}
