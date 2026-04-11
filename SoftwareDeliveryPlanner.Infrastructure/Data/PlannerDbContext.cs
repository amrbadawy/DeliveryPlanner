using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Data;

public class PlannerDbContext : DbContext
{
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<TeamMember> Resources { get; set; }
    public DbSet<Adjustment> Adjustments { get; set; }
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<CalendarDay> Calendar { get; set; }
    public DbSet<Allocation> Allocations { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<LookupValue> Lookups { get; set; }

    private readonly string _dbPath;
    private const string DbPathEnvVar = "PLANNER_DB_PATH";

    public PlannerDbContext(DbContextOptions<PlannerDbContext> options) : base(options)
    {
        _dbPath = ResolveDbPath();
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
    }

    public PlannerDbContext()
    {
        _dbPath = ResolveDbPath();
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
    }

    private static string ResolveDbPath()
    {
        var envPath = Environment.GetEnvironmentVariable(DbPathEnvVar);
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return Path.GetFullPath(envPath);
        }

        var folder = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(folder, "data", "planner.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseSqlite($"Data Source={_dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>()
            .HasIndex(t => t.TaskId)
            .IsUnique();

        modelBuilder.Entity<TaskItem>()
            .Property(t => t.TaskId)
            .ValueGeneratedNever();

        modelBuilder.Entity<TeamMember>()
            .HasIndex(r => r.ResourceId)
            .IsUnique();

        modelBuilder.Entity<TeamMember>()
            .Property(r => r.ResourceId)
            .ValueGeneratedNever();

        modelBuilder.Entity<Adjustment>()
            .HasOne(a => a.Resource)
            .WithMany()
            .HasForeignKey(a => a.ResourceId)
            .HasPrincipalKey(r => r.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Allocation>()
            .HasOne(a => a.Task)
            .WithMany()
            .HasForeignKey(a => a.TaskId)
            .HasPrincipalKey(t => t.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.HasIndex(h => h.StartDate);
            entity.HasIndex(h => h.EndDate);
        });

        modelBuilder.Entity<CalendarDay>()
            .HasIndex(c => c.DateKey)
            .IsUnique();

        modelBuilder.Entity<Setting>()
            .HasIndex(s => s.Key)
            .IsUnique();

        modelBuilder.Entity<LookupValue>(entity =>
        {
            entity.HasIndex(l => new { l.Category, l.Code }).IsUnique();
            entity.HasIndex(l => l.Category);
        });
    }

    public void InitializeDefaultData()
    {
        Database.EnsureCreated();

        if (Tasks.Any()) return;

        // ── Lookup values (source of truth for all categorical data) ──────────
        SeedLookupValues();

        Settings.AddRange(
            new Setting { Key = DomainConstants.SettingKeys.PlanStartDate, Value = "2026-05-01" },
            new Setting { Key = DomainConstants.SettingKeys.AtRiskThreshold, Value = "5" },
            new Setting { Key = DomainConstants.SettingKeys.WorkingWeek, Value = DomainConstants.WorkingWeek.SunThu }
        );

        Resources.AddRange(
            new TeamMember { ResourceId = "DEV-001", ResourceName = "Developer 1", Role = DomainConstants.ResourceRole.Developer, Team = DomainConstants.DefaultTeam, StartDate = new DateTime(2026, 4, 12), Notes = "Initial team" },
            new TeamMember { ResourceId = "DEV-002", ResourceName = "Developer 2", Role = DomainConstants.ResourceRole.Developer, Team = DomainConstants.DefaultTeam, StartDate = new DateTime(2026, 4, 12), Notes = "Initial team" },
            new TeamMember { ResourceId = "DEV-003", ResourceName = "Developer 3", Role = DomainConstants.ResourceRole.Developer, Team = DomainConstants.DefaultTeam, StartDate = new DateTime(2026, 4, 12), Notes = "Initial team" },
            new TeamMember { ResourceId = "DEV-004", ResourceName = "Developer 4", Role = DomainConstants.ResourceRole.Developer, Team = DomainConstants.DefaultTeam, StartDate = new DateTime(2026, 6, 1), Notes = "Phase 2" },
            new TeamMember { ResourceId = "DEV-005", ResourceName = "Developer 5", Role = DomainConstants.ResourceRole.Developer, Team = DomainConstants.DefaultTeam, StartDate = new DateTime(2026, 6, 1), Notes = "Phase 2" }
        );

        // Consolidated holiday records — multi-day holidays use date ranges (12 → 7)
        Holidays.AddRange(
            new Holiday { HolidayName = "عيد رأس السنة الميلادية", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 1), HolidayType = DomainConstants.HolidayType.National },
            new Holiday { HolidayName = "يوم التأسيس السعودي", StartDate = new DateTime(2026, 2, 22), EndDate = new DateTime(2026, 2, 22), HolidayType = DomainConstants.HolidayType.National },
            new Holiday { HolidayName = "عيد الفطر المبارك", StartDate = new DateTime(2026, 3, 30), EndDate = new DateTime(2026, 4, 2), HolidayType = DomainConstants.HolidayType.Religious },
            new Holiday { HolidayName = "يوم عرفات وعيد الأضحى المبارك", StartDate = new DateTime(2026, 5, 27), EndDate = new DateTime(2026, 5, 30), HolidayType = DomainConstants.HolidayType.Religious },
            new Holiday { HolidayName = "يوم عاشوراء", StartDate = new DateTime(2026, 9, 17), EndDate = new DateTime(2026, 9, 17), HolidayType = DomainConstants.HolidayType.Religious },
            new Holiday { HolidayName = "اليوم الوطني", StartDate = new DateTime(2026, 9, 23), EndDate = new DateTime(2026, 9, 23), HolidayType = DomainConstants.HolidayType.National },
            new Holiday { HolidayName = "يوم العلم", StartDate = new DateTime(2026, 3, 11), EndDate = new DateTime(2026, 3, 11), HolidayType = DomainConstants.HolidayType.National }
        );

        Tasks.AddRange(
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

        SaveChanges();
    }

    /// <summary>
    /// Seeds all lookup values. These are the source of truth for categorical data
    /// that would traditionally be modeled as enums. The architecture rule is:
    /// never use enums — always store categorical values as <see cref="LookupValue"/> rows.
    /// </summary>
    private void SeedLookupValues()
    {
        Lookups.AddRange(
            // TaskStatus
            Lk(DomainConstants.LookupCategory.TaskStatus, DomainConstants.TaskStatus.NotStarted, "Not Started", 1),
            Lk(DomainConstants.LookupCategory.TaskStatus, DomainConstants.TaskStatus.InProgress, "In Progress", 2),
            Lk(DomainConstants.LookupCategory.TaskStatus, DomainConstants.TaskStatus.Completed, "Completed", 3),

            // DeliveryRisk
            Lk(DomainConstants.LookupCategory.DeliveryRisk, DomainConstants.DeliveryRisk.OnTrack, "On Track", 1),
            Lk(DomainConstants.LookupCategory.DeliveryRisk, DomainConstants.DeliveryRisk.AtRisk, "At Risk", 2),
            Lk(DomainConstants.LookupCategory.DeliveryRisk, DomainConstants.DeliveryRisk.Late, "Late", 3),

            // HolidayType
            Lk(DomainConstants.LookupCategory.HolidayType, DomainConstants.HolidayType.National, "National", 1),
            Lk(DomainConstants.LookupCategory.HolidayType, DomainConstants.HolidayType.Religious, "Religious", 2),
            Lk(DomainConstants.LookupCategory.HolidayType, DomainConstants.HolidayType.Company, "Company", 3),

            // AdjustmentType
            Lk(DomainConstants.LookupCategory.AdjustmentType, DomainConstants.AdjustmentType.Vacation, "Vacation", 1),
            Lk(DomainConstants.LookupCategory.AdjustmentType, DomainConstants.AdjustmentType.Training, "Training", 2),
            Lk(DomainConstants.LookupCategory.AdjustmentType, DomainConstants.AdjustmentType.SickLeave, "Sick Leave", 3),
            Lk(DomainConstants.LookupCategory.AdjustmentType, DomainConstants.AdjustmentType.Other, "Other", 4),

            // ActiveStatus
            Lk(DomainConstants.LookupCategory.ActiveStatus, DomainConstants.ActiveStatus.Yes, "Yes", 1),
            Lk(DomainConstants.LookupCategory.ActiveStatus, DomainConstants.ActiveStatus.No, "No", 2),

            // ResourceRole
            Lk(DomainConstants.LookupCategory.ResourceRole, DomainConstants.ResourceRole.Developer, "Developer", 1),
            Lk(DomainConstants.LookupCategory.ResourceRole, DomainConstants.ResourceRole.SeniorDeveloper, "Senior Developer", 2),
            Lk(DomainConstants.LookupCategory.ResourceRole, DomainConstants.ResourceRole.TechLead, "Tech Lead", 3),
            Lk(DomainConstants.LookupCategory.ResourceRole, DomainConstants.ResourceRole.QA, "QA", 4),

            // WorkingWeek
            Lk(DomainConstants.LookupCategory.WorkingWeek, DomainConstants.WorkingWeek.SunThu, "Sunday - Thursday", 1),
            Lk(DomainConstants.LookupCategory.WorkingWeek, DomainConstants.WorkingWeek.MonFri, "Monday - Friday", 2)
        );
    }

    private static LookupValue Lk(string category, string code, string displayName, int sortOrder) =>
        new() { Category = category, Code = code, DisplayName = displayName, SortOrder = sortOrder };
}
