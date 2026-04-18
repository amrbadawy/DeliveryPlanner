using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class SchedulerSnapshotConfiguration : IEntityTypeConfiguration<SchedulerSnapshot>
{
    public void Configure(EntityTypeBuilder<SchedulerSnapshot> builder)
    {
        builder.ToTable("SchedulerSnapshots", "planning");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.RunTimestamp)
            .IsRequired();

        builder.Property(s => s.OnTrackCount)
            .IsRequired();

        builder.Property(s => s.AtRiskCount)
            .IsRequired();

        builder.Property(s => s.LateCount)
            .IsRequired();

        builder.Property(s => s.TotalTasks)
            .IsRequired();
    }
}
