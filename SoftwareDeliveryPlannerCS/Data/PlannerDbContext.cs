using Microsoft.EntityFrameworkCore;
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

    private readonly string _dbPath;

    public PlannerDbContext(DbContextOptions<PlannerDbContext> options) : base(options)
    {
        var folder = AppDomain.CurrentDomain.BaseDirectory;
        _dbPath = Path.Combine(folder, "data", "planner.db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
    }

    public PlannerDbContext()
    {
        var folder = AppDomain.CurrentDomain.BaseDirectory;
        _dbPath = Path.Combine(folder, "data", "planner.db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
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

        modelBuilder.Entity<TeamMember>()
            .HasIndex(r => r.ResourceId)
            .IsUnique();

        modelBuilder.Entity<Holiday>()
            .HasIndex(h => h.HolidayDate);

        modelBuilder.Entity<CalendarDay>()
            .HasIndex(c => c.DateKey)
            .IsUnique();

        modelBuilder.Entity<Setting>()
            .HasIndex(s => s.Key)
            .IsUnique();
    }

    public void InitializeDefaultData()
    {
        Database.EnsureCreated();

        if (Tasks.Any()) return;

        Settings.AddRange(
            new Setting { Key = "plan_start_date", Value = "2026-05-01" },
            new Setting { Key = "at_risk_threshold", Value = "5" },
            new Setting { Key = "working_week", Value = "sun_thu" }
        );

        Resources.AddRange(
            new TeamMember { ResourceId = "DEV-001", ResourceName = "Developer 1", Role = "Developer", Team = "Delivery", StartDate = new DateTime(2026, 4, 12), Notes = "Initial team" },
            new TeamMember { ResourceId = "DEV-002", ResourceName = "Developer 2", Role = "Developer", Team = "Delivery", StartDate = new DateTime(2026, 4, 12), Notes = "Initial team" },
            new TeamMember { ResourceId = "DEV-003", ResourceName = "Developer 3", Role = "Developer", Team = "Delivery", StartDate = new DateTime(2026, 4, 12), Notes = "Initial team" },
            new TeamMember { ResourceId = "DEV-004", ResourceName = "Developer 4", Role = "Developer", Team = "Delivery", StartDate = new DateTime(2026, 6, 1), Notes = "Phase 2" },
            new TeamMember { ResourceId = "DEV-005", ResourceName = "Developer 5", Role = "Developer", Team = "Delivery", StartDate = new DateTime(2026, 6, 1), Notes = "Phase 2" }
        );

        Holidays.AddRange(
            new Holiday { HolidayName = "عيد رأس السنة الميلادية", HolidayDate = new DateTime(2026, 1, 1), HolidayType = "National" },
            new Holiday { HolidayName = "يوم التأسيس السعودي", HolidayDate = new DateTime(2026, 2, 22), HolidayType = "National" },
            new Holiday { HolidayName = "عيد الفطر المبارك - يوم 1", HolidayDate = new DateTime(2026, 3, 30), HolidayType = "Religious" },
            new Holiday { HolidayName = "عيد الفطر المبارك - يوم 2", HolidayDate = new DateTime(2026, 3, 31), HolidayType = "Religious" },
            new Holiday { HolidayName = "عيد الفطر المبارك - يوم 3", HolidayDate = new DateTime(2026, 4, 1), HolidayType = "Religious" },
            new Holiday { HolidayName = "عيد الفطر المبارك - يوم 4", HolidayDate = new DateTime(2026, 4, 2), HolidayType = "Religious" },
            new Holiday { HolidayName = "يوم عرفات", HolidayDate = new DateTime(2026, 5, 27), HolidayType = "Religious" },
            new Holiday { HolidayName = "عيد الأضحى المبارك - يوم 1", HolidayDate = new DateTime(2026, 5, 28), HolidayType = "Religious" },
            new Holiday { HolidayName = "عيد الأضحى المبارك - يوم 2", HolidayDate = new DateTime(2026, 5, 29), HolidayType = "Religious" },
            new Holiday { HolidayName = "عيد الأضحى المبارك - يوم 3", HolidayDate = new DateTime(2026, 5, 30), HolidayType = "Religious" },
            new Holiday { HolidayName = "يوم عاشوراء", HolidayDate = new DateTime(2026, 9, 17), HolidayType = "Religious" },
            new Holiday { HolidayName = "اليوم الوطني", HolidayDate = new DateTime(2026, 9, 23), HolidayType = "National" }
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
}
