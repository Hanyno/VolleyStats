using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using VolleyStats.Domain;

namespace VolleyStats.Data
{
    public class VolleyStatsDbContext : DbContext
    {
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<Player> Players => Set<Player>();

        private readonly string _dbPath;

        public VolleyStatsDbContext()
        {
            _dbPath = DatabasePaths.GetTeamsDatabasePath();
        }

        public VolleyStatsDbContext(string dbPath)
        {
            _dbPath = dbPath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var birthDateConverter = new ValueConverter<DateTimeOffset?, string?>(
                toDb => toDb.HasValue
                    ? toDb.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : null,
                fromDb => string.IsNullOrWhiteSpace(fromDb)
                    ? (DateTimeOffset?)null
                    : DateTimeOffset.ParseExact(fromDb, "yyyy-MM-dd", CultureInfo.InvariantCulture)
            );

            var nullableBoolToIntConverter = new ValueConverter<bool?, int?>(
                toDb => toDb.HasValue ? (toDb.Value ? 1 : 0) : null,
                fromDb => fromDb.HasValue ? (fromDb.Value != 0) : null
            );

            // --- Team ---
            modelBuilder.Entity<Team>(e =>
            {
                e.ToTable("Teams");
                e.HasKey(t => t.Id);

                e.Property(t => t.TeamCode).IsRequired();
                e.Property(t => t.Name).IsRequired();

                e.Property(t => t.CoachName);
                e.Property(t => t.AssistantCoachName);
                e.Property(t => t.Abbreviation);
                e.Property(t => t.CharacterEncoding);


                e.HasMany(t => t.Players)
                 .WithOne(p => p.Team)
                 .HasForeignKey(p => p.TeamId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // --- Player ---
            modelBuilder.Entity<Player>(e =>
            {
                e.ToTable("Players");
                e.HasKey(p => p.Id);

                e.Property(p => p.TeamId).IsRequired();
                e.Property(p => p.JerseyNumber).IsRequired();

                e.Property(p => p.ExternalPlayerId);
                e.Property(p => p.LastName);
                e.Property(p => p.FirstName);

                e.Property(p => p.BirthDate)
                 .HasConversion(birthDateConverter);

                e.Property(p => p.HeightCm);

                e.Property(p => p.Position);

                e.Property(p => p.PlayerRole);
                e.Property(p => p.NickName);

                e.Property(p => p.IsForeign)
                 .HasConversion(nullableBoolToIntConverter);

                e.Property(p => p.TransferredOut)
                 .HasConversion(nullableBoolToIntConverter);

                e.Property(p => p.BirthDateSerial);

                e.HasIndex(p => p.TeamId).HasDatabaseName("IX_Players_TeamId");
            });
        }
    }

}
