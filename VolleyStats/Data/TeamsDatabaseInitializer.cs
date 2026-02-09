using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolleyStats.Data
{
    public static class TeamsDatabaseInitializer
    {
        public static void EnsureCreated()
        {
            using var db = new VolleyStatsDbContext();
            db.Database.EnsureCreated();
        }
    }
}

