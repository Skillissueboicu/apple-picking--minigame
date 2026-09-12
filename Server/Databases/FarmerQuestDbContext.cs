using Microsoft.EntityFrameworkCore;
using FarmerQuest.Server.Models;
using FarmerQuest.Server.GameFeatures.Farm;

namespace FarmerQuest.Server.Databases;

/// <summary>
/// EF Core database context for FarmerQuest: users, social, sessions, and farm saves
/// </summary>
public class FarmerQuestDbContext : DbContext
{
    public FarmerQuestDbContext(DbContextOptions<FarmerQuestDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<PlayerStat> PlayerStats => Set<PlayerStat>();
    public DbSet<BadgeProgress> BadgeProgresses => Set<BadgeProgress>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<GameSessionPlayer> GameSessionPlayers => Set<GameSessionPlayer>();
    public DbSet<GameSessionInvitation> GameSessionInvitations => Set<GameSessionInvitation>();
    public DbSet<FarmSave> FarmSaves => Set<FarmSave>();
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();

    public DbSet<BuildingPlacement> BuildingPlacements => Set<BuildingPlacement>(); //Mahmoud her

    // Configures keys, indexes, FK lengths, and cascade rules
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            // Same length on all UserId FKs (avoids SQL error 1753 on BadgeProgress --> PlayerStats)
            entity.Property(u => u.Id).HasMaxLength(64);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Team).HasMaxLength(100);
        });

        modelBuilder.Entity<PlayerStat>(entity =>
        {
            entity.HasKey(p => p.UserId);
            entity.Property(p => p.UserId).HasMaxLength(64);
            entity.HasOne(p => p.User)
                .WithOne(u => u.PlayerStat)
                .HasForeignKey<PlayerStat>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BadgeProgress>(entity =>
        {
            entity.HasKey(b => b.BadgeProgressId);
            // MUST match PlayerStats.UserId maxLength
            entity.Property(b => b.UserId).HasMaxLength(64);
            entity.Property(b => b.BadgeKey).HasMaxLength(64);
            entity.Property(b => b.Tier).HasMaxLength(16);
            entity.HasOne(b => b.PlayerStat)
                .WithMany(p => p.BadgeProgresses)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(b => new { b.UserId, b.BadgeKey }).IsUnique();
        });

        modelBuilder.Entity<GameSession>(entity =>
        {
            entity.HasKey(s => s.SessionId);
            entity.Property(s => s.SessionId).HasMaxLength(64);
            entity.Property(s => s.Code).HasMaxLength(16);
            entity.Property(s => s.GameKind).HasMaxLength(20);
            entity.Property(s => s.FarmSaveId).HasMaxLength(64);
            entity.Property(s => s.HostUserId).HasMaxLength(64);
            entity.Property(s => s.ActiveDriverUserId).HasMaxLength(64);
            entity.Property(s => s.Status).HasMaxLength(12);
            entity.HasIndex(s => s.Code).IsUnique();
            entity.HasIndex(s => s.Status);
            entity.HasIndex(s => s.FarmSaveId);
            entity.HasOne<FarmSave>()
                .WithMany()
                .HasForeignKey(s => s.FarmSaveId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<FarmSave>(entity =>
        {
            entity.HasKey(s => s.SaveId);
            entity.Property(s => s.SaveId).HasMaxLength(64);
            entity.Property(s => s.OwnerUserId).HasMaxLength(64);
            entity.Property(s => s.DisplayName).HasMaxLength(80);
            entity.HasIndex(s => s.OwnerUserId);
            entity.HasIndex(s => new { s.OwnerUserId, s.LastPlayedAt });
        });

        modelBuilder.Entity<GameSessionPlayer>(entity =>
        {
            entity.HasKey(p => p.GameSessionPlayerId);
            entity.Property(p => p.SessionId).HasMaxLength(64);
            entity.Property(p => p.UserId).HasMaxLength(64);
            entity.Property(p => p.Name).HasMaxLength(100);
            entity.HasOne(p => p.Session)
                .WithMany(s => s.Players)
                .HasForeignKey(p => p.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(p => new { p.SessionId, p.UserId }).IsUnique();
        });

        modelBuilder.Entity<GameSessionInvitation>(entity =>
        {
            entity.HasKey(i => i.InvitationId);
            entity.Property(i => i.SessionId).HasMaxLength(64);
            entity.Property(i => i.InvitedUserId).HasMaxLength(64);
            entity.Property(i => i.InvitedByUserId).HasMaxLength(64);
            entity.Property(i => i.InvitedByName).HasMaxLength(100);
            entity.Property(i => i.Status).HasMaxLength(12);
            entity.HasOne(i => i.Session)
                .WithMany()
                .HasForeignKey(i => i.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(i => new { i.SessionId, i.InvitedUserId, i.Status });
            entity.HasIndex(i => new { i.InvitedUserId, i.Status });
        });

        modelBuilder.Entity<FriendRequest>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.FromUserId).HasMaxLength(64);
            entity.Property(r => r.ToUserId).HasMaxLength(64);
            entity.Property(r => r.Status).HasMaxLength(12);
            entity.HasIndex(r => new { r.ToUserId, r.Status });
            entity.HasIndex(r => new { r.FromUserId, r.Status });
            entity.HasIndex(r => new { r.FromUserId, r.ToUserId, r.Status });
        });

        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.UserIdA).HasMaxLength(64);
            entity.Property(f => f.UserIdB).HasMaxLength(64);
            entity.HasIndex(f => new { f.UserIdA, f.UserIdB }).IsUnique();
            entity.HasIndex(f => f.UserIdA);
            entity.HasIndex(f => f.UserIdB);
        });

        modelBuilder.Entity<UserBlock>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.BlockerUserId).HasMaxLength(64);
            entity.Property(b => b.BlockedUserId).HasMaxLength(64);
            entity.HasIndex(b => new { b.BlockerUserId, b.BlockedUserId }).IsUnique();
            entity.HasIndex(b => b.BlockedUserId);
        });
        modelBuilder.Entity<BuildingPlacement>() //Mahmoud her, prøver at sætte placement limit
        .HasIndex(b => new { b.UserId, b.X, b.Y })
        .IsUnique();
    }
}
