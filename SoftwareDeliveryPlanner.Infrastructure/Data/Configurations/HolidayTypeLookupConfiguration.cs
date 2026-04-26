using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class HolidayTypeLookupConfiguration : IEntityTypeConfiguration<HolidayTypeLookup>
{
    public void Configure(EntityTypeBuilder<HolidayTypeLookup> builder)
    {
        builder.ToTable("HolidayTypes", "resource", t =>
            t.HasCheckConstraint("CK_HolidayTypes_Code_Format", "Code = UPPER(Code) AND Code NOT LIKE '% %'"));

        builder.HasKey(x => x.Code);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(x => x.SortOrder);

        builder.HasData(
            new HolidayTypeLookup { Code = DomainConstants.HolidayType.National, DisplayName = "National", SortOrder = 1, IsActive = true },
            new HolidayTypeLookup { Code = DomainConstants.HolidayType.Religious, DisplayName = "Religious", SortOrder = 2, IsActive = true },
            new HolidayTypeLookup { Code = DomainConstants.HolidayType.Company, DisplayName = "Company", SortOrder = 3, IsActive = true }
        );
    }
}
