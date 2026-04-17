using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Infrastructure.Data;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Tests.Infrastructure;

/// <summary>
/// Provides helper methods for setting up test databases on the shared
/// SQL Server instance. Creates fresh databases and seeds default data.
/// </summary>
internal static class TestDatabaseHelper
{
    /// <summary>
    /// Creates DbContextOptions configured for an isolated SQL Server database.
    /// Uses EnsureCreated to create the schema (including HasData seeds) and
    /// optionally seeds additional default data. Returns both the options and
    /// the connection string so callers can also create a ReadOnly factory.
    /// </summary>
    internal static (DbContextOptions<PlannerDbContext> Options, string ConnectionString) CreateOptions(
        SqlServerFixture fixture,
        bool seedData = true)
    {
        var connectionString = fixture.CreateDatabaseConnectionString();

        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        using var db = new PlannerDbContext(options);
        db.Database.EnsureCreated();

        if (seedData)
        {
            SeedDefaultData(db);
        }

        return (options, connectionString);
    }

    /// <summary>
    /// Seeds the same default data that was previously in InitializeDefaultData().
    /// Lookup data is already handled by migrations via HasData().
    /// </summary>
    internal static void SeedDefaultData(PlannerDbContext db)
    {
        if (db.Tasks.Any()) return;

        db.Settings.AddRange(
            new Setting { Key = DomainConstants.SettingKeys.PlanStartDate, Value = "2026-05-01" },
            new Setting { Key = DomainConstants.SettingKeys.AtRiskThreshold, Value = "5" },
            new Setting { Key = DomainConstants.SettingKeys.WorkingWeek, Value = DomainConstants.WorkingWeek.SunThu }
        );

        db.Resources.AddRange(
            new TeamMember { ResourceId = "DEV-001", ResourceName = "Developer 1", Role = DomainConstants.ResourceRole.Developer, Team = DomainConstants.DefaultTeam, StartDate = new DateTime(2026, 4, 12), Notes = "Initial team" },
            new TeamMember { ResourceId = "DEV-002", ResourceName = "Developer 2", Role = DomainConstants.ResourceRole.Developer, Team = DomainConstants.DefaultTeam, StartDate = new DateTime(2026, 4, 12), Notes = "Initial team" },
            new TeamMember { ResourceId = "DEV-003", ResourceName = "Developer 3", Role = DomainConstants.ResourceRole.Developer, Team = DomainConstants.DefaultTeam, StartDate = new DateTime(2026, 4, 12), Notes = "Initial team" },
            new TeamMember { ResourceId = "DEV-004", ResourceName = "Developer 4", Role = DomainConstants.ResourceRole.Developer, Team = DomainConstants.DefaultTeam, StartDate = new DateTime(2026, 6, 1), Notes = "Phase 2" },
            new TeamMember { ResourceId = "DEV-005", ResourceName = "Developer 5", Role = DomainConstants.ResourceRole.Developer, Team = DomainConstants.DefaultTeam, StartDate = new DateTime(2026, 6, 1), Notes = "Phase 2" }
        );

        db.Holidays.AddRange(
            new Holiday { HolidayName = "عيد رأس السنة الميلادية", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 1), HolidayType = DomainConstants.HolidayType.National },
            new Holiday { HolidayName = "يوم التأسيس السعودي", StartDate = new DateTime(2026, 2, 22), EndDate = new DateTime(2026, 2, 22), HolidayType = DomainConstants.HolidayType.National },
            new Holiday { HolidayName = "عيد الفطر المبارك", StartDate = new DateTime(2026, 3, 30), EndDate = new DateTime(2026, 4, 2), HolidayType = DomainConstants.HolidayType.Religious },
            new Holiday { HolidayName = "يوم عرفات وعيد الأضحى المبارك", StartDate = new DateTime(2026, 5, 27), EndDate = new DateTime(2026, 5, 30), HolidayType = DomainConstants.HolidayType.Religious },
            new Holiday { HolidayName = "يوم عاشوراء", StartDate = new DateTime(2026, 9, 17), EndDate = new DateTime(2026, 9, 17), HolidayType = DomainConstants.HolidayType.Religious },
            new Holiday { HolidayName = "اليوم الوطني", StartDate = new DateTime(2026, 9, 23), EndDate = new DateTime(2026, 9, 23), HolidayType = DomainConstants.HolidayType.National },
            new Holiday { HolidayName = "يوم العلم", StartDate = new DateTime(2026, 3, 11), EndDate = new DateTime(2026, 3, 11), HolidayType = DomainConstants.HolidayType.National }
        );

        db.Tasks.AddRange(
            new TaskItem { TaskId = "SVC-001", ServiceName = "ادارة المحتوى اخبار والحملات التوعوية", DevEstimation = 53, MaxDev = 3.5, Priority = 5 },
            new TaskItem { TaskId = "SVC-002", ServiceName = "التكامل مع منصة تحميل المرحلة الثانية", DevEstimation = 18, MaxDev = 2.0, StrictDate = new DateTime(2026, 7, 23), Priority = 5 },
            new TaskItem { TaskId = "SVC-003", ServiceName = "اضافه / حذف موظف على جهة بعد اصدار الترخيص", DevEstimation = 40, MaxDev = 3.5, Priority = 5 },
            new TaskItem { TaskId = "SVC-004", ServiceName = "التكامل مع نظام الموارد البشرية للملف الشخصي", DevEstimation = 27, MaxDev = 2.5, Priority = 5 },
            new TaskItem { TaskId = "SVC-005", ServiceName = "طلب تغيير تعديل حقول تقرير الكشف والتسرب", DevEstimation = 5, MaxDev = 2.0, Priority = 5 },
            new TaskItem { TaskId = "SVC-006", ServiceName = "تعديل التراخيص الاضافية", DevEstimation = 10, MaxDev = 2.0, Priority = 5 },
            new TaskItem { TaskId = "SVC-007", ServiceName = "لوحات التحكم والرقابة", DevEstimation = 20, MaxDev = 2.5, Priority = 5 },
            new TaskItem { TaskId = "SVC-008", ServiceName = "خدمة تنفيذ الاصلاحات", DevEstimation = 92, MaxDev = 4.5, Priority = 5 },
            new TaskItem { TaskId = "SVC-009", ServiceName = "تجديد الترخيص منصة نما", DevEstimation = 12, MaxDev = 2.0, Priority = 5 },
            new TaskItem { TaskId = "SVC-010", ServiceName = "ادارة المستخدمين", DevEstimation = 22, MaxDev = 2.5, Priority = 5 },
            new TaskItem { TaskId = "SVC-011", ServiceName = "الغاء الترخيص", DevEstimation = 37, MaxDev = 3.0, Priority = 5 },
            new TaskItem { TaskId = "SVC-012", ServiceName = "اتمتة الانذارات على الجهات المعتمدة", DevEstimation = 15, MaxDev = 2.0, Priority = 5 },
            new TaskItem { TaskId = "SVC-013", ServiceName = "التكامل مع المؤسسة العامة للري", DevEstimation = 29, MaxDev = 3.0, Priority = 5 }
        );

        db.SaveChanges();
    }
}
