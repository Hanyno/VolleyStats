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

        // --- Kontext zápasu ---
        public MatchSet Set { get; set; } = null!;
        public Rally Rally { get; set; } = null!;
        public int OrderInRally { get; set; }

        // --- Strana + hráč ---
        public TeamSide Side { get; set; }
        public MatchPlayer Player { get; set; } = null!;

        // --- Základní DV parametry (strukturované) ---
        public BasicSkill Skill { get; set; }
        public EvaluationSymbol Eval { get; set; }

        // --- Rozšířené DV parametry jako prosté stringy ---
        public string? AttackCombinationCode { get; set; }
        public string? SetterCallCode { get; set; }
        public string? AttackZoneCode { get; set; }
        public string? SubZoneCode { get; set; }

        // libovolné další DV suffixy (např. H2, O, X, D)
        public string? ExtraFlags { get; set; }

        // --- Reálný čas akce (nebo null když nechceš používat) ---
        public DateTime? RealTime { get; set; }

        // --- Kompletní DV kód pro export/import ---
        public string? RawCode { get; set; }
    }

}
