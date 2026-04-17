using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("TeamMembers", "resource");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ResourceId)
            .IsRequired()
            .HasMaxLength(20)
            .ValueGeneratedNever();

        builder.Property(r => r.ResourceName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(r => r.Role)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Team)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Active)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(r => r.Notes)
            .HasMaxLength(500);

        builder.HasIndex(r => r.ResourceId)
            .IsUnique();

        // Configure Adjustments navigation via backing field
        builder.HasMany(r => r.Adjustments)
            .WithOne()
            .HasForeignKey(a => a.ResourceId)
            .HasPrincipalKey(r => r.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(r => r.Adjustments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
