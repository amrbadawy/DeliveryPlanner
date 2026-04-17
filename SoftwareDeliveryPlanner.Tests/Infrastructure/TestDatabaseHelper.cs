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
            TeamMember.Create("DEV-001", "Developer 1", DomainConstants.ResourceRole.Developer, DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 4, 12), notes: "Initial team"),
            TeamMember.Create("DEV-002", "Developer 2", DomainConstants.ResourceRole.Developer, DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 4, 12), notes: "Initial team"),
            TeamMember.Create("DEV-003", "Developer 3", DomainConstants.ResourceRole.Developer, DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 4, 12), notes: "Initial team"),
            TeamMember.Create("DEV-004", "Developer 4", DomainConstants.ResourceRole.Developer, DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 6, 1), notes: "Phase 2"),
            TeamMember.Create("DEV-005", "Developer 5", DomainConstants.ResourceRole.Developer, DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 6, 1), notes: "Phase 2")
        );

        db.Holidays.AddRange(
            Holiday.Create("عيد رأس السنة الميلادية", new DateTime(2026, 1, 1), new DateTime(2026, 1, 1), DomainConstants.HolidayType.National),
            Holiday.Create("يوم التأسيس السعودي", new DateTime(2026, 2, 22), new DateTime(2026, 2, 22), DomainConstants.HolidayType.National),
            Holiday.Create("عيد الفطر المبارك", new DateTime(2026, 3, 30), new DateTime(2026, 4, 2), DomainConstants.HolidayType.Religious),
            Holiday.Create("يوم عرفات وعيد الأضحى المبارك", new DateTime(2026, 5, 27), new DateTime(2026, 5, 30), DomainConstants.HolidayType.Religious),
            Holiday.Create("يوم عاشوراء", new DateTime(2026, 9, 17), new DateTime(2026, 9, 17), DomainConstants.HolidayType.Religious),
            Holiday.Create("اليوم الوطني", new DateTime(2026, 9, 23), new DateTime(2026, 9, 23), DomainConstants.HolidayType.National),
            Holiday.Create("يوم العلم", new DateTime(2026, 3, 11), new DateTime(2026, 3, 11), DomainConstants.HolidayType.National)
        );

        db.Tasks.AddRange(
            TaskItem.Create("SVC-001", "ادارة المحتوى اخبار والحملات التوعوية", 53, 3.5, 5),
            TaskItem.Create("SVC-002", "التكامل مع منصة تحميل المرحلة الثانية", 18, 2.0, 5, strictDate: new DateTime(2026, 7, 23)),
            TaskItem.Create("SVC-003", "اضافه / حذف موظف على جهة بعد اصدار الترخيص", 40, 3.5, 5),
            TaskItem.Create("SVC-004", "التكامل مع نظام الموارد البشرية للملف الشخصي", 27, 2.5, 5),
            TaskItem.Create("SVC-005", "طلب تغيير تعديل حقول تقرير الكشف والتسرب", 5, 2.0, 5),
            TaskItem.Create("SVC-006", "تعديل التراخيص الاضافية", 10, 2.0, 5),
            TaskItem.Create("SVC-007", "لوحات التحكم والرقابة", 20, 2.5, 5),
            TaskItem.Create("SVC-008", "خدمة تنفيذ الاصلاحات", 92, 4.5, 5),
            TaskItem.Create("SVC-009", "تجديد الترخيص منصة نما", 12, 2.0, 5),
            TaskItem.Create("SVC-010", "ادارة المستخدمين", 22, 2.5, 5),
            TaskItem.Create("SVC-011", "الغاء الترخيص", 37, 3.0, 5),
            TaskItem.Create("SVC-012", "اتمتة الانذارات على الجهات المعتمدة", 15, 2.0, 5),
            TaskItem.Create("SVC-013", "التكامل مع المؤسسة العامة للري", 29, 3.0, 5)
        );

        db.SaveChanges();
    }
}
