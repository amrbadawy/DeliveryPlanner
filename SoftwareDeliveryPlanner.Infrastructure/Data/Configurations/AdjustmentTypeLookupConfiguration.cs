using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class AdjustmentTypeLookupConfiguration : IEntityTypeConfiguration<AdjustmentTypeLookup>
{
    public void Configure(EntityTypeBuilder<AdjustmentTypeLookup> builder)
    {
        builder.ToTable("AdjustmentTypes", "resource", t =>
            t.HasCheckConstraint("CK_AdjustmentTypes_Code_Format", "Code = UPPER(Code) AND Code NOT LIKE '% %'"));

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
            new AdjustmentTypeLookup { Code = DomainConstants.AdjustmentType.Vacation, DisplayName = "Vacation", SortOrder = 1, IsActive = true },
            new AdjustmentTypeLookup { Code = DomainConstants.AdjustmentType.Training, DisplayName = "Training", SortOrder = 2, IsActive = true },
            new AdjustmentTypeLookup { Code = DomainConstants.AdjustmentType.SickLeave, DisplayName = "Sick Leave", SortOrder = 3, IsActive = true },
            new AdjustmentTypeLookup { Code = DomainConstants.AdjustmentType.Other, DisplayName = "Other", SortOrder = 4, IsActive = true }
        );
    }
}
