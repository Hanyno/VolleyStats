using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VolleyStats.Enums;

namespace VolleyStats.Domain
{
    public class MatchEvent
    {
        public int Id { get; set; }

        public MatchSet Set { get; set; } = null!;
        public Rally Rally { get; set; } = null!;
        public int OrderInRally { get; set; }

        public TeamSide Side { get; set; }
        public MatchPlayer Player { get; set; } = null!;

        public BasicSkill Skill { get; set; }
        public EvaluationSymbol Eval { get; set; }

        public string? AttackCombinationCode { get; set; }
        public string? SetterCallCode { get; set; }
        public string? AttackZoneCode { get; set; }
        public string? SubZoneCode { get; set; }

        public string? ExtraFlags { get; set; }

        public DateTime? RealTime { get; set; }

        public string? RawCode { get; set; }
        public int? PlayerId { get; set; }

    }

}
