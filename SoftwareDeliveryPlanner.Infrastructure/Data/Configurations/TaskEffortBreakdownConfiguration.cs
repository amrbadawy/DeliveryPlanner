using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

internal class TaskEffortBreakdownConfiguration : IEntityTypeConfiguration<TaskEffortBreakdown>
{
    public void Configure(EntityTypeBuilder<TaskEffortBreakdown> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TaskId).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Role).IsRequired().HasMaxLength(10);
        builder.Property(e => e.EstimationDays);
        builder.Property(e => e.OverlapPct);
        builder.Property(e => e.SortOrder);

        builder.Property(e => e.MinSeniority)
            .HasMaxLength(20);
    }
}
