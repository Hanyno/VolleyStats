using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolleyStats.Services
{
    using VolleyStats.Domain;
    public interface ITeamsService
    {
        List<Team> GetAllTeamsWithPlayers();
        void SaveTeam(Team team);
        void DeleteTeam(int teamId);
    }
}
