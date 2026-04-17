using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data;

/// <summary>
/// Read-only DbContext that connects to a read replica (or the primary with no-tracking).
/// <list type="bullet">
///   <item>Tracking is disabled globally (<see cref="QueryTrackingBehavior.NoTracking"/>).</item>
///   <item>Change detection is turned off for performance.</item>
///   <item><see cref="SaveChanges()"/> and <see cref="SaveChangesAsync(CancellationToken)"/> throw
///         <see cref="InvalidOperationException"/> to prevent accidental writes.</item>
/// </list>
/// Use <see cref="PlannerDbContext"/> for all write operations.
/// </summary>
internal class ReadOnlyPlannerDbContext : DbContext
{
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<TeamMember> Resources { get; set; }
    public DbSet<Adjustment> Adjustments { get; set; }
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<CalendarDay> Calendar { get; set; }
    public DbSet<Allocation> Allocations { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<LookupValue> Lookups { get; set; }

    public ReadOnlyPlannerDbContext(DbContextOptions<ReadOnlyPlannerDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Reuse the same entity configurations as the read-write context.
        // This ensures both contexts map to the same schema.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlannerDbContext).Assembly);
    }

    // ─── Sealed write operations — prevent accidental mutations ────────────

    public override int SaveChanges()
        => throw new InvalidOperationException(
            "This is a read-only context. Write operations are not permitted. Use PlannerDbContext for writes.");

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
        => throw new InvalidOperationException(
            "This is a read-only context. Write operations are not permitted. Use PlannerDbContext for writes.");

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            "This is a read-only context. Write operations are not permitted. Use PlannerDbContext for writes.");

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            "This is a read-only context. Write operations are not permitted. Use PlannerDbContext for writes.");
}
