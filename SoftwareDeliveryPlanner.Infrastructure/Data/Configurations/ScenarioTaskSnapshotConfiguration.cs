using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class ScenarioTaskSnapshotConfiguration : IEntityTypeConfiguration<ScenarioTaskSnapshot>
{
    public void Configure(EntityTypeBuilder<ScenarioTaskSnapshot> builder)
    {
        builder.ToTable("ScenarioTaskSnapshots", "planning");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TaskId)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.ServiceName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.Priority)
            .IsRequired();

        builder.Property(s => s.MaxResource)
            .IsRequired();

        builder.Property(s => s.Phase)
            .HasMaxLength(50);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.DeliveryRisk)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.AssignedResourceId)
            .HasMaxLength(500);

        builder.Property(s => s.DependsOnTaskIds)
            .HasMaxLength(1000);

        builder.HasMany(s => s.EffortSnapshots)
            .WithOne(e => e.TaskSnapshot)
            .HasForeignKey(e => e.ScenarioTaskSnapshotId);

        builder.Metadata.FindNavigation("EffortSnapshots")!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
