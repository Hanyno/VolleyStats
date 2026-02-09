using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VolleyStats.Domain;

namespace VolleyStats.Application
{
    public interface ITeamRepository
    {
        Task<List<Team>> GetAllAsync();
        Task SaveAsync(Team team);
        Task DeleteAsync(int teamId);
    }
}
