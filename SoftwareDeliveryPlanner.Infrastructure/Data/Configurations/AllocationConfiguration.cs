using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class AllocationConfiguration : IEntityTypeConfiguration<Allocation>
{
    public void Configure(EntityTypeBuilder<Allocation> builder)
    {
        builder.ToTable("Allocations");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AllocationId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.TaskId)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.ServiceStatus)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasOne(a => a.Task)
            .WithMany()
            .HasForeignKey(a => a.TaskId)
            .HasPrincipalKey(t => t.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
