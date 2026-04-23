using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

internal class ScenarioEffortSnapshotConfiguration : IEntityTypeConfiguration<ScenarioEffortSnapshot>
{
    public void Configure(EntityTypeBuilder<ScenarioEffortSnapshot> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Role).IsRequired().HasMaxLength(10);
        builder.Property(e => e.EstimationDays);
        builder.Property(e => e.OverlapPct);
        builder.Property(e => e.MaxFte).HasDefaultValue(1.0);
        builder.Property(e => e.SortOrder);

        builder.HasOne(e => e.TaskSnapshot)
            .WithMany(s => s.EffortSnapshots)
            .HasForeignKey(e => e.ScenarioTaskSnapshotId);

        builder.ToTable("ScenarioEffortSnapshots", "planning");
    }
}
