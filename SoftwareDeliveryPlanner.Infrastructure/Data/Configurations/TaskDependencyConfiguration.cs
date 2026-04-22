using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
{
    public void Configure(EntityTypeBuilder<TaskDependency> builder)
    {
        builder.ToTable("TaskDependencies", "task");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.TaskId).IsRequired().HasMaxLength(20);
        builder.Property(d => d.PredecessorTaskId).IsRequired().HasMaxLength(20);
        builder.Property(d => d.Type).IsRequired().HasMaxLength(5);

        builder.HasIndex(d => new { d.TaskId, d.PredecessorTaskId }).IsUnique();
    }
}
