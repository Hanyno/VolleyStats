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
        /// <summary>
        /// Vrátí všechny týmy včetně hráčů.
        /// </summary>
        List<Team> GetAllTeamsWithPlayers();

        /// <summary>
        /// Uloží tým (insert/update).
        /// </summary>
        void SaveTeam(Team team);

        /// <summary>
        /// Smaže tým podle Id.
        /// </summary>
        void DeleteTeam(int teamId);
    }
}
