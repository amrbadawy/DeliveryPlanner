using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class LookupValueConfiguration : IEntityTypeConfiguration<LookupValue>
{
    public void Configure(EntityTypeBuilder<LookupValue> builder)
    {
        builder.ToTable("LookupValues");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(l => new { l.Category, l.Code })
            .IsUnique();

        builder.HasIndex(l => l.Category);

        // ── Reference data seed ──────────────────────────────────────────────
        builder.HasData(
            // TaskStatus
            Lk(1, DomainConstants.LookupCategory.TaskStatus, DomainConstants.TaskStatus.NotStarted, "Not Started", 1),
            Lk(2, DomainConstants.LookupCategory.TaskStatus, DomainConstants.TaskStatus.InProgress, "In Progress", 2),
            Lk(3, DomainConstants.LookupCategory.TaskStatus, DomainConstants.TaskStatus.Completed, "Completed", 3),

            // DeliveryRisk
            Lk(4, DomainConstants.LookupCategory.DeliveryRisk, DomainConstants.DeliveryRisk.OnTrack, "On Track", 1),
            Lk(5, DomainConstants.LookupCategory.DeliveryRisk, DomainConstants.DeliveryRisk.AtRisk, "At Risk", 2),
            Lk(6, DomainConstants.LookupCategory.DeliveryRisk, DomainConstants.DeliveryRisk.Late, "Late", 3),

            // HolidayType
            Lk(7, DomainConstants.LookupCategory.HolidayType, DomainConstants.HolidayType.National, "National", 1),
            Lk(8, DomainConstants.LookupCategory.HolidayType, DomainConstants.HolidayType.Religious, "Religious", 2),
            Lk(9, DomainConstants.LookupCategory.HolidayType, DomainConstants.HolidayType.Company, "Company", 3),

            // AdjustmentType
            Lk(10, DomainConstants.LookupCategory.AdjustmentType, DomainConstants.AdjustmentType.Vacation, "Vacation", 1),
            Lk(11, DomainConstants.LookupCategory.AdjustmentType, DomainConstants.AdjustmentType.Training, "Training", 2),
            Lk(12, DomainConstants.LookupCategory.AdjustmentType, DomainConstants.AdjustmentType.SickLeave, "Sick Leave", 3),
            Lk(13, DomainConstants.LookupCategory.AdjustmentType, DomainConstants.AdjustmentType.Other, "Other", 4),

            // ActiveStatus
            Lk(14, DomainConstants.LookupCategory.ActiveStatus, DomainConstants.ActiveStatus.Yes, "Yes", 1),
            Lk(15, DomainConstants.LookupCategory.ActiveStatus, DomainConstants.ActiveStatus.No, "No", 2),

            // ResourceRole
            Lk(16, DomainConstants.LookupCategory.ResourceRole, DomainConstants.ResourceRole.Developer, "Developer", 1),
            Lk(17, DomainConstants.LookupCategory.ResourceRole, DomainConstants.ResourceRole.SeniorDeveloper, "Senior Developer", 2),
            Lk(18, DomainConstants.LookupCategory.ResourceRole, DomainConstants.ResourceRole.TechLead, "Tech Lead", 3),
            Lk(19, DomainConstants.LookupCategory.ResourceRole, DomainConstants.ResourceRole.QA, "QA", 4),

            // WorkingWeek
            Lk(20, DomainConstants.LookupCategory.WorkingWeek, DomainConstants.WorkingWeek.SunThu, "Sunday - Thursday", 1),
            Lk(21, DomainConstants.LookupCategory.WorkingWeek, DomainConstants.WorkingWeek.MonFri, "Monday - Friday", 2)
        );
    }

    private static LookupValue Lk(int id, string category, string code, string displayName, int sortOrder) =>
        new() { Id = id, Category = category, Code = code, DisplayName = displayName, SortOrder = sortOrder, IsActive = true };
}
