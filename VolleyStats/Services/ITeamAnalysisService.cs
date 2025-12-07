using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VolleyStats.Enums;
using VolleyStats.DTO;

namespace VolleyStats.Services
{
    public interface ITeamAnalysisService
    {
        TeamBasicOverviewDto GetBasicOverview(
            int teamId,
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            int? competitionId = null,
            bool includeHome = true,
            bool includeAway = true,
            int? limitLastMatches = null);

        SkillAnalysisDto GetSkillAnalysis(
            int teamId,
            BasicSkill skill,
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            int? competitionId = null,
            bool includeHome = true,
            bool includeAway = true,
            int? limitLastMatches = null);

        IEnumerable<PlayerStatsDto> GetPlayersStats(
            int teamId,
            DateTime? fromUtc,
            DateTime? toUtc,
            int? competitionId,
            bool includeHome,
            bool includeAway,
            int? limitLastMatches);

    }
}
