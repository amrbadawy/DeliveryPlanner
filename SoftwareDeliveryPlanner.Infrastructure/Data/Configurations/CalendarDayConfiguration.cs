using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class CalendarDayConfiguration : IEntityTypeConfiguration<CalendarDay>
{
    public void Configure(EntityTypeBuilder<CalendarDay> builder)
    {
        builder.ToTable("CalendarDays");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.DayName)
            .HasMaxLength(20);

        builder.Property(c => c.HolidayName)
            .HasMaxLength(200);

        builder.HasIndex(c => c.DateKey)
            .IsUnique();
    }
}
