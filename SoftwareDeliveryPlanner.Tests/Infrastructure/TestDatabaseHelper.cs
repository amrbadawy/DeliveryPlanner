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
    /// On the first call the database is created from scratch and the options are
    /// cached on the fixture. Subsequent calls reset data (delete + reseed) which
    /// is ~20× faster than drop-and-recreate (~200 ms vs ~4 s).
    /// </summary>
    internal static (DbContextOptions<PlannerDbContext> Options, string ConnectionString) CreateOptions(
        SqlServerFixture fixture,
        bool seedData = true)
    {
        // Fast path: reuse the cached schema, just reset the data.
        if (fixture.CachedOptions is { } cached)
        {
            using var db = new PlannerDbContext(cached.Options);
            ClearData(db);
            if (seedData) SeedDefaultData(db);
            return cached;
        }

        // Slow path (first call only): create the database.
        var connectionString = fixture.CreateDatabaseConnectionString();

        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        using var seedDb = new PlannerDbContext(options);
        seedDb.Database.EnsureCreated();

        if (seedData)
        {
            SeedDefaultData(seedDb);
        }

        fixture.CachedOptions = (options, connectionString);
        return (options, connectionString);
    }

    /// <summary>
    /// Deletes all mutable application data in FK-safe order and re-seeds
    /// the default data. Lookup tables (HasData) are left untouched.
    /// This is ~20× faster than dropping and recreating the database.
    /// </summary>
    internal static void ResetData(PlannerDbContext db)
    {
        ClearData(db);
        SeedDefaultData(db);
    }

    /// <summary>
    /// Deletes all mutable application data in FK-safe order.
    /// Lookup tables seeded via HasData are left untouched.
    /// </summary>
    internal static void ClearData(PlannerDbContext db)
    {
        // Delete in child-first FK order so constraints are never violated.
        // Table names are schema-qualified to match EF Core's actual DDL.
        db.Database.ExecuteSqlRaw("DELETE FROM [planning].[ScenarioEffortSnapshots]");
        db.Database.ExecuteSqlRaw("DELETE FROM [planning].[ScenarioTaskSnapshots]");
        db.Database.ExecuteSqlRaw("DELETE FROM [scheduling].[Allocations]");
        db.Database.ExecuteSqlRaw("DELETE FROM [dbo].[TaskEffortBreakdowns]");
        db.Database.ExecuteSqlRaw("DELETE FROM [task].[TaskDependencies]");
        db.Database.ExecuteSqlRaw("DELETE FROM [task].[TaskNotes]");
        db.Database.ExecuteSqlRaw("DELETE FROM [notification].[RiskNotifications]");
        db.Database.ExecuteSqlRaw("DELETE FROM [audit].[AuditEntries]");
        db.Database.ExecuteSqlRaw("DELETE FROM [planning].[SchedulerSnapshots]");
        db.Database.ExecuteSqlRaw("DELETE FROM [filter].[SavedViews]");
        db.Database.ExecuteSqlRaw("DELETE FROM [scheduling].[CalendarDays]");
        db.Database.ExecuteSqlRaw("DELETE FROM [planning].[PlanScenarios]");
        db.Database.ExecuteSqlRaw("DELETE FROM [task].[TaskItems]");
        db.Database.ExecuteSqlRaw("DELETE FROM [resource].[Adjustments]");
        db.Database.ExecuteSqlRaw("DELETE FROM [resource].[TeamMembers]");
        db.Database.ExecuteSqlRaw("DELETE FROM [resource].[Holidays]");
        db.Database.ExecuteSqlRaw("DELETE FROM [scheduling].[Settings]");
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
            TeamMember.Create("DEV-005", "Developer 5", DomainConstants.ResourceRole.Developer, DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 6, 1), notes: "Phase 2"),
            TeamMember.Create("QA-001", "QA Engineer 1", DomainConstants.ResourceRole.QA, DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 4, 12), notes: "Initial team"),
            TeamMember.Create("QA-002", "QA Engineer 2", DomainConstants.ResourceRole.QA, DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 4, 12), notes: "Initial team"),
            TeamMember.Create("QA-003", "QA Engineer 3", DomainConstants.ResourceRole.QA, DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 4, 12), notes: "Initial team")
        );

        db.Holidays.AddRange(
            Holiday.Create("يوم التأسيس السعودي", new DateTime(2026, 2, 22), new DateTime(2026, 2, 22), DomainConstants.HolidayType.National),
            Holiday.Create("عيد الفطر المبارك", new DateTime(2026, 3, 30), new DateTime(2026, 4, 2), DomainConstants.HolidayType.Religious),
            Holiday.Create("يوم عرفات وعيد الأضحى المبارك", new DateTime(2026, 5, 27), new DateTime(2026, 5, 30), DomainConstants.HolidayType.Religious),
            Holiday.Create("اليوم الوطني", new DateTime(2026, 9, 23), new DateTime(2026, 9, 23), DomainConstants.HolidayType.National)
        );

        db.Tasks.AddRange(
            TaskItem.Create("SVC-001", "ادارة المحتوى اخبار والحملات التوعوية", 5, MakeBreakdown(43, 10)),
            TaskItem.Create("SVC-002", "التكامل مع منصة تحميل المرحلة الثانية", 5, MakeBreakdown(14, 4), strictDate: new DateTime(2026, 7, 23)),
            TaskItem.Create("SVC-003", "اضافه / حذف موظف على جهة بعد اصدار الترخيص", 5, MakeBreakdown(32, 8)),
            TaskItem.Create("SVC-004", "التكامل مع نظام الموارد البشرية للملف الشخصي", 5, MakeBreakdown(22, 5)),
            TaskItem.Create("SVC-005", "طلب تغيير تعديل حقول تقرير الكشف والتسرب", 5, MakeBreakdown(4, 1)),
            TaskItem.Create("SVC-006", "تعديل التراخيص الاضافية", 5, MakeBreakdown(8, 2)),
            TaskItem.Create("SVC-007", "لوحات التحكم والرقابة", 5, MakeBreakdown(16, 4)),
            TaskItem.Create("SVC-008", "خدمة تنفيذ الاصلاحات", 5, MakeBreakdown(74, 18)),
            TaskItem.Create("SVC-009", "تجديد الترخيص منصة نما", 5, MakeBreakdown(10, 2)),
            TaskItem.Create("SVC-010", "ادارة المستخدمين", 5, MakeBreakdown(18, 4)),
            TaskItem.Create("SVC-011", "الغاء الترخيص", 5, MakeBreakdown(30, 7)),
            TaskItem.Create("SVC-012", "اتمتة الانذارات على الجهات المعتمدة", 5, MakeBreakdown(12, 3)),
            TaskItem.Create("SVC-013", "التكامل مع المؤسسة العامة للري", 5, MakeBreakdown(23, 6))
        );

        db.SaveChanges();
    }

    /// <summary>Helper to create effort breakdown specs for test tasks.</summary>
    internal static List<EffortBreakdownSpec> MakeBreakdown(
        double devDays, double qaDays = 1, double overlapPct = 0)
    {
        return
        [
            new EffortBreakdownSpec("DEV", devDays, 0),
            new EffortBreakdownSpec("QA", qaDays > 0 ? qaDays : 1, overlapPct)
        ];
    }

    /// <summary>
    /// Helper to create multi-role effort breakdown specs (always includes DEV + QA as required).
    /// <paramref name="extras"/> are prepended in pipeline order before DEV + QA.
    /// </summary>
    internal static List<EffortBreakdownSpec> MakeMultiRoleBreakdown(
        double devDays,
        double qaDays,
        params (string Role, double Days)[] extras)
    {
        var list = extras.Select(e => new EffortBreakdownSpec(e.Role, e.Days, 0, 1.0)).ToList();
        list.Add(new EffortBreakdownSpec("DEV", devDays, 0));
        list.Add(new EffortBreakdownSpec("QA", qaDays > 0 ? qaDays : 1, 0));
        return list;
    }
}
