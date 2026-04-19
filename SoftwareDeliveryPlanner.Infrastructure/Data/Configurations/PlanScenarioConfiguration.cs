using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class PlanScenarioConfiguration : IEntityTypeConfiguration<PlanScenario>
{
    public void Configure(EntityTypeBuilder<PlanScenario> builder)
    {
        builder.ToTable("PlanScenarios", "planning");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ScenarioName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.TotalTasks)
            .IsRequired();

        builder.Property(s => s.OnTrackCount)
            .IsRequired();

        builder.Property(s => s.AtRiskCount)
            .IsRequired();

        builder.Property(s => s.LateCount)
            .IsRequired();

        builder.Property(s => s.TotalEstimation)
            .IsRequired();

        builder.Property(s => s.Notes)
            .HasMaxLength(1000);

        // Configure the one-to-many relationship with task snapshots
        builder.HasMany(s => s.TaskSnapshots)
            .WithOne(t => t.Scenario)
            .HasForeignKey(t => t.PlanScenarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.TaskSnapshots)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
