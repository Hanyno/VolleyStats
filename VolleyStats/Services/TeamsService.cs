using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VolleyStats.Data;
using VolleyStats.Domain;

namespace VolleyStats.Services
{
    public class TeamsService : ITeamsService
    {
        private readonly TeamsRepository _teamsRepository;

        public TeamsService(TeamsRepository teamsRepository)
        {
            _teamsRepository = teamsRepository;
        }

        public List<Team> GetAllTeamsWithPlayers()
        {
            return _teamsRepository.GetAllTeamsWithPlayers();
        }

        public void SaveTeam(Team team)
        {
            _teamsRepository.SaveTeam(team);
        }

        public void DeleteTeam(int teamId)
        {
            _teamsRepository.DeleteTeam(teamId);
        }
    }

}
