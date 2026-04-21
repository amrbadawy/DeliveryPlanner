using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("TaskItems", "task");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TaskId)
            .IsRequired()
            .HasMaxLength(20)
            .ValueGeneratedNever();

        builder.Property(t => t.ServiceName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(t => t.DeliveryRisk)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(t => t.AssignedResourceId)
            .HasMaxLength(500);

        builder.Property(t => t.DependsOnTaskIds)
            .HasMaxLength(500);

        builder.Property(t => t.Comments)
            .HasMaxLength(1000);

        builder.Property(t => t.Phase)
            .HasMaxLength(50);

        builder.Property(t => t.PreferredResourceIds)
            .HasMaxLength(200);

        builder.HasMany(t => t.EffortBreakdown)
            .WithOne(e => e.Task)
            .HasForeignKey(e => e.TaskId)
            .HasPrincipalKey(t => t.TaskId);

        builder.Metadata.FindNavigation("EffortBreakdown")!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(t => t.TotalEstimationDays);

        builder.HasIndex(t => t.TaskId)
            .IsUnique();
    }
}
