using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class AdjustmentConfiguration : IEntityTypeConfiguration<Adjustment>
{
    public void Configure(EntityTypeBuilder<Adjustment> builder)
    {
        builder.ToTable("Adjustments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ResourceId)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.AdjType)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(a => a.Notes)
            .HasMaxLength(500);

        builder.HasOne(a => a.Resource)
            .WithMany()
            .HasForeignKey(a => a.ResourceId)
            .HasPrincipalKey(r => r.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
