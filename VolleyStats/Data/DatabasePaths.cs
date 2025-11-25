using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace VolleyStats.Data
{
    public static class DatabasePaths
    {
        public static string GetTeamsDatabasePath()
        {
            var baseDir = AppContext.BaseDirectory;

            var seasonFolder = Path.Combine(baseDir, "Seasons", "My Season", "Data");
            if (!Directory.Exists(seasonFolder))
            {
                Directory.CreateDirectory(seasonFolder);
            }

            return Path.Combine(seasonFolder, "teams.db");
        }
    }
}
