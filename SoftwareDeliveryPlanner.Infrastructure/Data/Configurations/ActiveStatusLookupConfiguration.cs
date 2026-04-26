using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class ActiveStatusLookupConfiguration : IEntityTypeConfiguration<ActiveStatusLookup>
{
    public void Configure(EntityTypeBuilder<ActiveStatusLookup> builder)
    {
        builder.ToTable("ActiveStatuses", "resource", t =>
            t.HasCheckConstraint("CK_ActiveStatuses_Code_Format", "Code = UPPER(Code) AND Code NOT LIKE '% %'"));

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
            new ActiveStatusLookup { Code = DomainConstants.ActiveStatus.Yes, DisplayName = "Yes", SortOrder = 1, IsActive = true },
            new ActiveStatusLookup { Code = DomainConstants.ActiveStatus.No, DisplayName = "No", SortOrder = 2, IsActive = true }
        );
    }
}
