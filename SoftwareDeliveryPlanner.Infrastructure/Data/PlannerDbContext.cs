using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data;

internal class PlannerDbContext : DbContext
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

    public PlannerDbContext(DbContextOptions<PlannerDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlannerDbContext).Assembly);
    }
}
