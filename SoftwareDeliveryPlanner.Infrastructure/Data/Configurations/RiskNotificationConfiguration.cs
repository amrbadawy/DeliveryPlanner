using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class RiskNotificationConfiguration : IEntityTypeConfiguration<RiskNotification>
{
    public void Configure(EntityTypeBuilder<RiskNotification> builder)
    {
        builder.ToTable("RiskNotifications", "notification");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.TaskId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(n => n.ServiceName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.PreviousRisk)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(n => n.CurrentRisk)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.Property(n => n.IsRead)
            .IsRequired();

        builder.HasIndex(n => n.IsRead);
        builder.HasIndex(n => n.TaskId);
    }
}
