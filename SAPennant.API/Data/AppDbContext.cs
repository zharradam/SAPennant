using Microsoft.EntityFrameworkCore;
using SAPennant.API.Models;

namespace SAPennant.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<PennantMatch> PennantMatches { get; set; }
    public DbSet<Season> Seasons { get; set; }
    public DbSet<SyncLog> SyncLogs { get; set; }
    public DbSet<RoundStatus> RoundStatuses { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }
    public DbSet<HonourRoll> HonourRoll { get; set; }
    public DbSet<HonourRollNarrative> HonourRollNarratives { get; set; }
    public DbSet<PoolFinalistConfig> PoolFinalistConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PennantMatch>(entity =>
        {
            entity.HasIndex(e => e.PlayerName);
            entity.HasIndex(e => new { e.Year, e.IsFinals });
        });

        modelBuilder.Entity<Season>(entity =>
        {
            entity.Property(e => e.Year).ValueGeneratedNever();
        });

        modelBuilder.Entity<RoundStatus>(entity =>
        {
            entity.HasIndex(e => new { e.Year, e.Pool, e.Round }).IsUnique();
        });

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(e => e.Key);
        });
    }
}