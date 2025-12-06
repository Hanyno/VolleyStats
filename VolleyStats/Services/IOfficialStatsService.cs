using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VolleyStats.Domain;

namespace VolleyStats.Services
{
    /// <summary>
    /// Služba pro práci s oficiálními zápasy a statistikami.
    /// </summary>
    public interface IOfficialStatsService
    {
        /// <summary>
        /// Vrátí všechny naplánované (nedohrané) zápasy včetně týmů a jejich soupisek.
        /// </summary>
        List<Match> GetPlannedMatches();

        /// <summary>
        /// Uloží statistiky k danému zápasu (sety, výměny, eventy) a označí zápas jako dohraný.
        /// </summary>
        void SaveMatchStatistics(Match match);

        /// <summary>
        /// Nahraje/aktualizuje týmy a jejich hráče do official_stats DB.
        /// </summary>
        void UploadTeams(IEnumerable<Team> teams);

        List<Team> GetAllTeams();

        void UploadMatches(IEnumerable<Match> matches);

        void LoadMatchStatistics(Match match);
    }

}
