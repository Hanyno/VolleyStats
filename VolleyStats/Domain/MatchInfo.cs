using System;
using System.Collections.Generic;
using System.Text;

namespace VolleyStats.Domain
{
    public class MatchInfo
    {
        // first line in dvw
        public DateOnly Date { get; set; }
        public TimeOnly Time { get; set; }
        public string Seasson { get; set; }
        public string League { get; set; }
        public string Phase { get; set; }
        // idk neco tam je
        public int Day { get; set; }
        public string Number { get; set; }
        public int CharEncoding { get; set; }
        // idk neco tam je 2
        // second line in dvw
        public string? idk1 { get; set; }
        public string? idk2 { get; set; }
        public int WeirdDate { get; set; }
        public string? idk3 { get; set; }
        public string? idk4 { get; set; }
        public string? EndTime { get; set; }

    }
}
