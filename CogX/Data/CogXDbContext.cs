using Microsoft.EntityFrameworkCore;
using CogX.Models;

namespace CogX.Data
{
    public class CogXDbContext : DbContext
    {
        public CogXDbContext(DbContextOptions<CogXDbContext> options) 
            : base(options)
        {
        }

        public DbSet<Player> Players { get; set; }
        public DbSet<Lobby> Lobbies { get; set; }
        public DbSet<GameSession> GameSessions { get; set; }
        public DbSet<GameAction> GameActions { get; set; }
        public DbSet<LeaderboardEntry> Leaderboard { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration des relations
            modelBuilder.Entity<Lobby>()
                .HasOne(l => l.Host)
                .WithMany()
                .HasForeignKey(l => l.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Lobby>()
                .HasMany(l => l.Players)
                .WithMany()
                .UsingEntity(j => j.ToTable("LobbyPlayers"));

            modelBuilder.Entity<GameSession>()
                .HasOne(gs => gs.Lobby)
                .WithMany()
                .HasForeignKey(gs => gs.LobbyId);

            modelBuilder.Entity<GameAction>()
                .HasOne(ga => ga.GameSession)
                .WithMany()
                .HasForeignKey(ga => ga.GameSessionId);

            modelBuilder.Entity<GameAction>()
                .HasOne(ga => ga.Player)
                .WithMany()
                .HasForeignKey(ga => ga.PlayerId);

            modelBuilder.Entity<LeaderboardEntry>()
                .HasOne(le => le.Player)
                .WithMany()
                .HasForeignKey(le => le.PlayerId);
        }
    }
}
