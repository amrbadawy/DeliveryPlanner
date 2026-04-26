using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class WorkingWeekLookupConfiguration : IEntityTypeConfiguration<WorkingWeekLookup>
{
    public void Configure(EntityTypeBuilder<WorkingWeekLookup> builder)
    {
        builder.ToTable("WorkingWeeks", t =>
            t.HasCheckConstraint("CK_WorkingWeeks_Code_Format", "Code = UPPER(Code) AND Code NOT LIKE '% %'"));

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
            new WorkingWeekLookup { Code = DomainConstants.WorkingWeek.SunThu, DisplayName = "Sunday - Thursday", SortOrder = 1, IsActive = true },
            new WorkingWeekLookup { Code = DomainConstants.WorkingWeek.MonFri, DisplayName = "Monday - Friday", SortOrder = 2, IsActive = true }
        );
    }
}
