using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VolleyStats.Domain;

namespace VolleyStats.Application
{
    public class TeamsService
    {
        private readonly ITeamRepository _repo;

        public TeamsService(ITeamRepository repo)
        {
            _repo = repo;
        }

        public Task<List<Team>> GetTeamsAsync()
            => _repo.GetAllAsync();

        public Task SaveTeamAsync(Team team)
            => _repo.SaveAsync(team);

        public Task DeleteTeamAsync(int teamId)
            => _repo.DeleteAsync(teamId);
    }
}
