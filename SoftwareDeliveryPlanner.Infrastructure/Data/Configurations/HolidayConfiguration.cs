using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays", "resource");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.HolidayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(h => h.HolidayType)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(h => h.Notes)
            .HasMaxLength(500);

        // DurationDays is a computed property (no backing field) -- ignore it
        builder.Ignore(h => h.DurationDays);

        builder.HasIndex(h => h.StartDate);
        builder.HasIndex(h => h.EndDate);
    }
}
