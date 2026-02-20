using ApplicationTracker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ApplicationTracker.Infrastructure.Database;

public class TrackerDbContext : DbContext
{
    public DbSet<Application> Applications { get; set; }
    public DbSet<Interview> Interviews { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<StatusUpdate> StatusUpdates { get; set; }

    public TrackerDbContext(DbContextOptions<TrackerDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Application>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Status).HasConversion<string>();
        });

        modelBuilder.Entity<Interview>(e =>
        {
            e.HasKey(i => i.Id);
            e.HasIndex(i => i.ApplicationId);
        });

        modelBuilder.Entity<Note>(e =>
        {
            e.HasKey(n => n.Id);
            e.HasIndex(n => n.ApplicationId);
        });

        modelBuilder.Entity<StatusUpdate>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.ApplicationId);
            e.Property(s => s.FromStatus).HasConversion<string>();
            e.Property(s => s.ToStatus).HasConversion<string>();
        });
    }
}
