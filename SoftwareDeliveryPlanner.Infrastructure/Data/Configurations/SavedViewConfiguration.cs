using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class SavedViewConfiguration : IEntityTypeConfiguration<SavedView>
{
    public void Configure(EntityTypeBuilder<SavedView> builder)
    {
        builder.ToTable("SavedViews", "filter");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.PageKey)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(v => v.OwnerKey)
            .HasMaxLength(256);

        builder.Property(v => v.PayloadJson)
            .IsRequired();

        builder.Property(v => v.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        // Unique per owner + page so duplicate names can't accumulate within one scope.
        // HasFilter(null) overrides SQL Server's default "WHERE column IS NOT NULL" filter
        // for nullable columns — required so global views (OwnerKey=null) also enforce uniqueness.
        builder.HasIndex(v => new { v.OwnerKey, v.PageKey, v.Name })
            .IsUnique()
            .HasFilter(null);

        builder.HasIndex(v => new { v.OwnerKey, v.PageKey });

        // One default view at most per (OwnerKey, PageKey).
        // HasFilter(null) ensures nullable OwnerKey values are included in uniqueness checks.
        builder.HasIndex(v => new { v.OwnerKey, v.PageKey, v.IsDefault })
            .IsUnique()
            .HasFilter("[IsDefault] = 1");
    }
}
