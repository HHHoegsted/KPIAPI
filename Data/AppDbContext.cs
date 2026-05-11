using Microsoft.EntityFrameworkCore;
using KPIAPI.Domain.Entities;

namespace KPIAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Robot> Robots => Set<Robot>();
    public DbSet<RobotRun> RobotRuns => Set<RobotRun>();
    public DbSet<LogicalRun> LogicalRuns => Set<LogicalRun>();
    public DbSet<LogicalRunAttempt> LogicalRunAttempts => Set<LogicalRunAttempt>();
    public DbSet<RunEvent> RunEvents => Set<RunEvent>();
    public DbSet<KpiDefinition> KpiDefinitions => Set<KpiDefinition>();
    public DbSet<KpiMeasurement> KpiMeasurements => Set<KpiMeasurement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Indices
        modelBuilder.Entity<Robot>()
            .HasIndex(r => r.Key)
            .IsUnique();

        modelBuilder.Entity<RobotRun>()
            .HasIndex(r => new { r.RobotId, r.RunId })
            .IsUnique();

        modelBuilder.Entity<KpiDefinition>()
            .HasIndex(d => new { d.RobotId, d.Key })
            .IsUnique();

        modelBuilder.Entity<RobotRun>()
            .HasIndex(r => new { r.RobotId, r.StartTimeUtc });

        modelBuilder.Entity<LogicalRunAttempt>()
            .HasIndex(a => a.RobotRunId)
            .IsUnique();

        modelBuilder.Entity<LogicalRun>()
            .HasIndex(lr => new { lr.RobotId, lr.CreatedUtc });

        modelBuilder.Entity<LogicalRunAttempt>()
            .HasIndex(a => new { a.LogicalRunId, a.SortOrder });

        modelBuilder.Entity<KpiMeasurement>()
            .HasIndex(m => new { m.RunEventId, m.KpiDefinitionId });


        // Relationships
        modelBuilder.Entity<Robot>()
            .HasMany(r => r.Runs)
            .WithOne(rr => rr.Robot)
            .HasForeignKey(rr => rr.RobotId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Robot>()
            .HasMany(r => r.LogicalRuns)
            .WithOne(lr => lr.Robot)
            .HasForeignKey(lr => lr.RobotId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Robot>()
            .HasMany(r => r.KpiDefinitions)
            .WithOne(d => d.Robot)
            .HasForeignKey(d => d.RobotId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RobotRun>()
            .HasMany(r => r.Events)
            .WithOne(e => e.RobotRun)
            .HasForeignKey(e => e.RobotRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RobotRun>()
            .HasOne(r => r.LogicalRunAttempt)
            .WithOne(a => a.RobotRun)
            .HasForeignKey<LogicalRunAttempt>(a => a.RobotRunId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LogicalRun>()
            .HasMany(lr => lr.Attempts)
            .WithOne(a => a.LogicalRun)
            .HasForeignKey(a => a.LogicalRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RunEvent>()
            .HasMany(e => e.KpiMeasurements)
            .WithOne(m => m.RunEvent)
            .HasForeignKey(m => m.RunEventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KpiDefinition>()
            .HasMany(d => d.KpiMeasurements)
            .WithOne(m => m.KpiDefinition)
            .HasForeignKey(m => m.KpiDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<KpiMeasurement>()
            .Property(m => m.DecimalValue)
            .HasPrecision(18, 4);
    }
}
