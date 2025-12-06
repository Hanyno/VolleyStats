using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VolleyStats.Data;
using VolleyStats.Domain;

namespace VolleyStats.Services
{
    public class OfficialStatsService : IOfficialStatsService
    {
        private readonly OfficialMatchRepository _officialMatchRepository;

        public OfficialStatsService(OfficialMatchRepository officialMatchRepository)
        {
            _officialMatchRepository = officialMatchRepository;
        }

        public List<Match> GetPlannedMatches()
        {
            return _officialMatchRepository
                .GetPlannedMatches()
                .ToList();
        }

        public void SaveMatchStatistics(Match match)
        {
            _officialMatchRepository.SaveMatchStatistics(match);
        }

        public void UploadTeams(IEnumerable<Team> teams)
        {
            _officialMatchRepository.UploadTeams(teams);
        }

        public List<Team> GetAllTeams()
        {
            return _officialMatchRepository.GetAllTeams().ToList();
        }

        public void UploadMatches(IEnumerable<Match> matches)
        {
            _officialMatchRepository.UploadMatches(matches);
        }
        public void LoadMatchStatistics(Match match)
        {
            _officialMatchRepository.LoadMatchStatistics(match);
        }

    }

}
