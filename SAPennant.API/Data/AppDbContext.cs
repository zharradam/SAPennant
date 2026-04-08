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
    }
}