using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data;

/// <summary>
/// Read-only DbContext that connects to a read replica (or the primary with no-tracking).
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
    public DbSet<Role> Roles { get; set; }
    public DbSet<TaskNote> TaskNotes => Set<TaskNote>();
    public DbSet<SchedulerSnapshot> SchedulerSnapshots => Set<SchedulerSnapshot>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<RiskNotification> RiskNotifications => Set<RiskNotification>();
    public DbSet<PlanScenario> PlanScenarios => Set<PlanScenario>();
    public DbSet<ScenarioTaskSnapshot> ScenarioTaskSnapshots => Set<ScenarioTaskSnapshot>();
    public DbSet<TaskEffortBreakdown> TaskEffortBreakdowns => Set<TaskEffortBreakdown>();
    public DbSet<ScenarioEffortSnapshot> ScenarioEffortSnapshots => Set<ScenarioEffortSnapshot>();

    public ReadOnlyPlannerDbContext(DbContextOptions<ReadOnlyPlannerDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlannerDbContext).Assembly);
    }

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
