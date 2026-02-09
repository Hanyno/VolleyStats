using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using VolleyStats.Application;
using VolleyStats.Domain;
using VolleyStats.Enums;

namespace VolleyStats.Data.Repositories
{
    public class TeamsRepository : ITeamRepository
    {
        private readonly string _dbPath;

        public TeamsRepository()
        {
            _dbPath = DatabasePaths.GetTeamsDatabasePath();
        }

        private VolleyStatsDbContext CreateDb() => new VolleyStatsDbContext(_dbPath);

        public void SaveTeam(Team team)
        {
            using var db = CreateDb();

            if (team.Id == 0)
            {
                db.Teams.Add(team);
                db.SaveChanges();
            }
            else
            {
                db.Teams.Update(team);

                var existingPlayers = db.Players.Where(p => p.TeamId == team.Id);
                db.Players.RemoveRange(existingPlayers);

                db.SaveChanges();
            }

            foreach (var player in team.Players)
            {
                player.TeamId = team.Id;
                db.Players.Add(player);
            }

            db.SaveChanges();
        }

        public List<Team> GetAllTeamsWithPlayers()
        {
            using var db = CreateDb();

            return db.Teams
                .Include(t => t.Players)
                .AsNoTracking()
                .ToList();
        }

        public void DeleteTeam(int teamId)
        {
            using var db = CreateDb();

            var team = db.Teams.FirstOrDefault(t => t.Id == teamId);
            if (team == null) return;

            db.Teams.Remove(team);
            db.SaveChanges();
        }

        public Task<List<Team>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task SaveAsync(Team team)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(int teamId)
        {
            throw new NotImplementedException();
        }
    }
}

