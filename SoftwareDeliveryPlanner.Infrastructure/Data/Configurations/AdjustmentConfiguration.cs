using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class AdjustmentConfiguration : IEntityTypeConfiguration<Adjustment>
{
    public void Configure(EntityTypeBuilder<Adjustment> builder)
    {
        builder.ToTable("Adjustments", "resource");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ResourceId)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.AdjType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Notes)
            .HasMaxLength(500);

        builder.HasOne<AdjustmentTypeLookup>()
            .WithMany()
            .HasForeignKey(a => a.AdjType)
            .HasPrincipalKey(x => x.Code)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to TeamMembers is configured via TeamMemberConfiguration.HasMany(Adjustments)
    }
}
