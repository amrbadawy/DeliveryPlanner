using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Infrastructure.Data;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data;

/// <summary>
/// Seeds default application data (settings, resources, holidays, tasks) on first run.
/// Lookup/reference data is handled by EF Core HasData() in migrations.
/// This seeder handles environment-specific data that may vary per deployment.
/// </summary>
internal sealed class DatabaseSeeder : IDatabaseSeeder
{
    private readonly IDbContextFactory<PlannerDbContext> _dbFactory;

    public DatabaseSeeder(IDbContextFactory<PlannerDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        // Idempotent: skip if data already exists
        if (await db.Tasks.AnyAsync(cancellationToken))
            return;

        SeedSettings(db);
        SeedResources(db);
        SeedHolidays(db);
        SeedTasks(db);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static void SeedSettings(PlannerDbContext db)
    {
        db.Settings.AddRange(
            new Setting { Key = DomainConstants.SettingKeys.PlanStartDate, Value = "2026-05-01" },
            new Setting { Key = DomainConstants.SettingKeys.AtRiskThreshold, Value = "5" },
            new Setting { Key = DomainConstants.SettingKeys.WorkingWeek, Value = DomainConstants.WorkingWeek.SunThu },
            new Setting { Key = DomainConstants.SettingKeys.SchedulingStrategy, Value = DomainConstants.SchedulingStrategy.PriorityFirst }
        );
    }

    private static void SeedResources(PlannerDbContext db)
    {
        db.Resources.AddRange(
            TeamMember.Create("DEV-001", "Developer 1", DomainConstants.ResourceRole.Developer, DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 4, 12), notes: "Initial team", seniorityLevel: DomainConstants.Seniority.Senior),
            TeamMember.Create("DEV-002", "Developer 2", DomainConstants.ResourceRole.Developer, DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 4, 12), notes: "Initial team", seniorityLevel: DomainConstants.Seniority.Mid),
            TeamMember.Create("DEV-003", "Developer 3", DomainConstants.ResourceRole.Developer, DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 4, 12), notes: "Initial team", seniorityLevel: DomainConstants.Seniority.Mid),
            TeamMember.Create("DEV-004", "Developer 4", DomainConstants.ResourceRole.Developer, DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 6, 1), notes: "Phase 2", seniorityLevel: DomainConstants.Seniority.Junior),
            TeamMember.Create("DEV-005", "Developer 5", DomainConstants.ResourceRole.Developer, DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 6, 1), notes: "Phase 2", seniorityLevel: DomainConstants.Seniority.Junior),
            TeamMember.Create("QA-001", "QA Engineer 1", DomainConstants.ResourceRole.QA, DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 4, 12), notes: "Initial team", seniorityLevel: DomainConstants.Seniority.Senior),
            TeamMember.Create("QA-002", "QA Engineer 2", DomainConstants.ResourceRole.QA, DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 4, 12), notes: "Initial team", seniorityLevel: DomainConstants.Seniority.Mid),
            TeamMember.Create("QA-003", "QA Engineer 3", DomainConstants.ResourceRole.QA, DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 6, 1), notes: "Phase 2", seniorityLevel: DomainConstants.Seniority.Junior)
        );
    }

    private static void SeedHolidays(PlannerDbContext db)
    {
        db.Holidays.AddRange(
            Holiday.Create("يوم التأسيس السعودي", new DateTime(2026, 2, 22), DomainConstants.HolidayType.National),
            Holiday.Create("عيد الفطر المبارك", new DateTime(2026, 3, 30), new DateTime(2026, 4, 2), DomainConstants.HolidayType.Religious),
            Holiday.Create("يوم عرفات وعيد الأضحى المبارك", new DateTime(2026, 5, 27), new DateTime(2026, 5, 30), DomainConstants.HolidayType.Religious),
            Holiday.Create("اليوم الوطني", new DateTime(2026, 9, 23), DomainConstants.HolidayType.National)
        );
    }

    private static void SeedTasks(PlannerDbContext db)
    {
        db.Tasks.AddRange(
            TaskItem.Create("SVC-001", "ادارة المحتوى اخبار والحملات التوعوية", 3.5, 5,
                [new EffortBreakdownSpec("DEV", 40, 0), new EffortBreakdownSpec("QA", 10, 20)], phase: "Phase 1"),
            TaskItem.Create("SVC-002", "التكامل مع منصة تحميل المرحلة الثانية", 2.0, 5,
                [new EffortBreakdownSpec("DEV", 14, 0), new EffortBreakdownSpec("QA", 4, 25)], strictDate: new DateTime(2026, 7, 23), phase: "Phase 1"),
            TaskItem.Create("SVC-003", "اضافه / حذف موظف على جهة بعد اصدار الترخيص", 3.5, 5,
                [new EffortBreakdownSpec("BA", 2, 0), new EffortBreakdownSpec("DEV", 30, 0), new EffortBreakdownSpec("QA", 8, 20)], phase: "Phase 1"),
            TaskItem.Create("SVC-004", "التكامل مع نظام الموارد البشرية للملف الشخصي", 2.5, 5,
                [new EffortBreakdownSpec("SA", 2, 0), new EffortBreakdownSpec("DEV", 20, 0), new EffortBreakdownSpec("QA", 5, 20)], phase: "Phase 1"),
            TaskItem.Create("SVC-005", "طلب تغيير تعديل حقول تقرير الكشف والتسرب", 2.0, 5,
                [new EffortBreakdownSpec("DEV", 3, 0), new EffortBreakdownSpec("QA", 2, 0)], phase: "Phase 1"),
            TaskItem.Create("SVC-006", "تعديل التراخيص الاضافية", 2.0, 5,
                [new EffortBreakdownSpec("DEV", 7, 0), new EffortBreakdownSpec("QA", 3, 0)], phase: "Phase 1"),
            TaskItem.Create("SVC-007", "لوحات التحكم والرقابة", 2.5, 5,
                [new EffortBreakdownSpec("DEV", 15, 0), new EffortBreakdownSpec("QA", 5, 20)], phase: "Phase 2"),
            TaskItem.Create("SVC-008", "خدمة تنفيذ الاصلاحات", 4.5, 5,
                [new EffortBreakdownSpec("BA", 5, 0), new EffortBreakdownSpec("UX", 7, 0), new EffortBreakdownSpec("DEV", 60, 0), new EffortBreakdownSpec("QA", 20, 15)], phase: "Phase 2"),
            TaskItem.Create("SVC-009", "تجديد الترخيص منصة نما", 2.0, 5,
                [new EffortBreakdownSpec("DEV", 9, 0), new EffortBreakdownSpec("QA", 3, 0)], phase: "Phase 2"),
            TaskItem.Create("SVC-010", "ادارة المستخدمين", 2.5, 5,
                [new EffortBreakdownSpec("DEV", 16, 0), new EffortBreakdownSpec("QA", 6, 20)], phase: "Phase 2"),
            TaskItem.Create("SVC-011", "الغاء الترخيص", 3.0, 5,
                [new EffortBreakdownSpec("DEV", 28, 0), new EffortBreakdownSpec("QA", 9, 20)], phase: "Phase 2"),
            TaskItem.Create("SVC-012", "اتمتة الانذارات على الجهات المعتمدة", 2.0, 5,
                [new EffortBreakdownSpec("DEV", 11, 0), new EffortBreakdownSpec("QA", 4, 0)], phase: "Phase 2"),
            TaskItem.Create("SVC-013", "التكامل مع المؤسسة العامة للري", 3.0, 5,
                [new EffortBreakdownSpec("DEV", 22, 0), new EffortBreakdownSpec("QA", 7, 20)], phase: "Phase 2")
        );
    }
}
