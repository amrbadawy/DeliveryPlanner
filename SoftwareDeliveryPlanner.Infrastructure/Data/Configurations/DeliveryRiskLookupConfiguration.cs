using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class DeliveryRiskLookupConfiguration : IEntityTypeConfiguration<DeliveryRiskLookup>
{
    public void Configure(EntityTypeBuilder<DeliveryRiskLookup> builder)
    {
        builder.ToTable("DeliveryRisks", t =>
            t.HasCheckConstraint("CK_DeliveryRisks_Code_Format", "Code = UPPER(Code) AND Code NOT LIKE '% %'"));

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
            new DeliveryRiskLookup { Code = DomainConstants.DeliveryRisk.OnTrack, DisplayName = "On Track", SortOrder = 1, IsActive = true },
            new DeliveryRiskLookup { Code = DomainConstants.DeliveryRisk.AtRisk, DisplayName = "At Risk", SortOrder = 2, IsActive = true },
            new DeliveryRiskLookup { Code = DomainConstants.DeliveryRisk.Late, DisplayName = "Late", SortOrder = 3, IsActive = true }
        );
    }
}
