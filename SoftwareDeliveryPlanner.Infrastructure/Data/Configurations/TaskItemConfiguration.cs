using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("TaskItems");

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
            .HasMaxLength(20);

        builder.Property(t => t.DependsOnTaskIds)
            .HasMaxLength(500);

        builder.Property(t => t.Comments)
            .HasMaxLength(1000);

        builder.HasIndex(t => t.TaskId)
            .IsUnique();
    }
}
