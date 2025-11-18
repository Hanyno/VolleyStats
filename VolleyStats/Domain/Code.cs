using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolleyStats.Domain
{
    public abstract class Code
    {
        // --- raw data ---
        public string RawLine { get; }

        // --- basic info ---
        public string RawCode { get; }
        public char SP { get; }
        public char PR { get; }

        // --- coordinates ---
        public CoordinatesPair? Start { get; }
        public CoordinatesPair? Middle { get; }
        public CoordinatesPair? End { get; }

        // --- metadata ---
        public DateTime? RecordedAt { get; }
        public int? SetNumber { get; }
        public int? HomeSetterZone { get; }
        public int? AwaySetterZone { get; }

        // --- video metadata ---
        public string? VideoFile { get; }
        public int? VideoSecond { get; }

        // --- zones on court ---
        public int[] HomeZones { get; }
        public int[] AwayZones { get; }

        protected Code(
            string rawLine,
            string rawCode,
            char sp,
            char pr,
            CoordinatesPair? start,
            CoordinatesPair? middle,
            CoordinatesPair? end,
            DateTime? recordedAt,
            int? setNumber,
            int? homeSetterZone,
            int? awaySetterZone,
            string videoFile,
            int? videoSecond,
            int[] homeZones,
            int[] awayZones)
        {
            RawLine = rawLine;
            RawCode = rawCode;
            SP = sp;
            PR = pr;
            Start = start;
            Middle = middle;
            End = end;
            RecordedAt = recordedAt;
            SetNumber = setNumber;
            HomeSetterZone = homeSetterZone;
            AwaySetterZone = awaySetterZone;
            VideoFile = videoFile ?? string.Empty;
            VideoSecond = videoSecond;
            HomeZones = homeZones ?? Array.Empty<int>();
            AwayZones = awayZones ?? Array.Empty<int>();
        }

        public override string ToString()
        {
            return $"{GetType().Name}: Code={RawCode}, Time={RecordedAt?.ToString("HH:mm:ss") ?? "null"}, Video={VideoSecond}s, Set={SetNumber}";
        }
    }
}
