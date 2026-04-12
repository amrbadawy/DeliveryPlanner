using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Data;

public class PlannerDbContext : DbContext
{
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<TeamMember> Resources { get; set; }
    public DbSet<Adjustment> Adjustments { get; set; }
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<CalendarDay> Calendar { get; set; }
    public DbSet<Allocation> Allocations { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<LookupValue> Lookups { get; set; }

    public PlannerDbContext(DbContextOptions<PlannerDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlannerDbContext).Assembly);
    }
}
