using System;
using System.Collections.Generic;
using System.Text;

namespace VolleyStats.Models
{
    public class MatchComments
    {
        public string CommentSummarry { get; set; } = "No comment";
        public string? MatchDesc { get; set; }
        public string? HomeCoachComment { get; set; }
        public string? AwayCoachComment { get; set; }
    }
}
