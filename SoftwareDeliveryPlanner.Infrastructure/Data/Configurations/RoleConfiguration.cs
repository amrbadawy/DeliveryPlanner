using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "resource");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(r => r.Code)
            .IsUnique();

        builder.HasData(
            new Role { Id = 1, Code = DomainConstants.ResourceRole.Developer, DisplayName = "Developer", IsActive = true, SortOrder = 1 },
            new Role { Id = 2, Code = DomainConstants.ResourceRole.SeniorDeveloper, DisplayName = "Senior Developer", IsActive = true, SortOrder = 2 },
            new Role { Id = 3, Code = DomainConstants.ResourceRole.TechLead, DisplayName = "Tech Lead", IsActive = true, SortOrder = 3 },
            new Role { Id = 4, Code = DomainConstants.ResourceRole.QA, DisplayName = "QA", IsActive = true, SortOrder = 4 }
        );
    }
}
