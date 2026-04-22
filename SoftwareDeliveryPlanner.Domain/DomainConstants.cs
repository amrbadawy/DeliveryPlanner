namespace SoftwareDeliveryPlanner.Domain;

/// <summary>
/// Centralizes magic strings used across the domain, application, and infrastructure layers.
/// Values here must match the <see cref="SoftwareDeliveryPlanner.Models.LookupValue"/> rows
/// seeded in the database. The DB is the source of truth; these constants provide compile-time safety.
/// </summary>
public static class DomainConstants
{
    /// <summary>
    /// Category names used to group <see cref="SoftwareDeliveryPlanner.Models.LookupValue"/> rows.
    /// </summary>
    public static class LookupCategory
    {
        public const string TaskStatus = "TaskStatus";
        public const string DeliveryRisk = "DeliveryRisk";
        public const string HolidayType = "HolidayType";
        public const string AdjustmentType = "AdjustmentType";
        public const string ActiveStatus = "ActiveStatus";
        public const string ResourceRole = "ResourceRole";
        public const string WorkingWeek = "WorkingWeek";
    }

    /// <summary>Task scheduling status values.</summary>
    public static class TaskStatus
    {
        public const string NotStarted = "Not Started";
        public const string InProgress = "In Progress";
        public const string Completed = "Completed";
    }

    /// <summary>Delivery risk assessment labels.</summary>
    public static class DeliveryRisk
    {
        public const string OnTrack = "On Track";
        public const string AtRisk = "At Risk";
        public const string Late = "Late";
    }

    /// <summary>Keys stored in the Settings table.</summary>
    public static class SettingKeys
    {
        public const string PlanStartDate = "plan_start_date";
        public const string AtRiskThreshold = "at_risk_threshold";
        public const string WorkingWeek = "working_week";
        public const string LastSchedulerRun = "last_scheduler_run";
        public const string BaselineDate = "baseline_date";
        public const string SchedulingStrategy = "scheduling_strategy";
    }

    /// <summary>Holiday classification types.</summary>
    public static class HolidayType
    {
        public const string National = "National";
        public const string Religious = "Religious";
        public const string Company = "Company";
    }

    /// <summary>Adjustment classification types.</summary>
    public static class AdjustmentType
    {
        public const string Vacation = "Vacation";
        public const string Training = "Training";
        public const string SickLeave = "Sick Leave";
        public const string Other = "Other";
    }

    /// <summary>Active/inactive status for resources.</summary>
    public static class ActiveStatus
    {
        public const string Yes = "Yes";
        public const string No = "No";
    }

    /// <summary>Team member roles.</summary>
    public static class ResourceRole
    {
        public const string Developer = "DEV";
        public const string QA = "QA";
        public const string SA = "SA";
        public const string BA = "BA";
        public const string UX = "UX";
        public const string UI = "UI";

        /// <summary>
        /// Fixed pipeline execution order: BA → SA → UX → UI → DEV → QA.
        /// The scheduler processes effort phases in this order.
        /// Only roles present in a task's breakdown are executed; absent roles are skipped.
        /// </summary>
        public static readonly IReadOnlyList<string> PipelineOrder = new[]
        {
            BA,        // 1 – Business Analysis
            SA,        // 2 – Solution Architecture
            UX,        // 3 – User Experience Design
            UI,        // 4 – UI Design
            Developer, // 5 – Development
            QA         // 6 – Quality Assurance
        };

        /// <summary>Required roles that every task must include in its effort breakdown.</summary>
        public static readonly IReadOnlySet<string> RequiredRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Developer, QA };

        /// <summary>All valid role codes.</summary>
        public static readonly IReadOnlySet<string> AllRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { BA, SA, UX, UI, Developer, QA };

        /// <summary>Returns the pipeline sort order for a role (lower = earlier).</summary>
        public static int GetPipelineSortOrder(string role) =>
            PipelineOrder is List<string> list ? (list.IndexOf(role) is int idx and >= 0 ? idx + 1 : int.MaxValue) : int.MaxValue;

        /// <summary>Human-readable display name for a role code.</summary>
        public static string GetDisplayName(string role) => role switch
        {
            BA => "Business Analyst",
            SA => "Solution Architect",
            UX => "UX Designer",
            UI => "UI Designer",
            Developer => "Developer",
            QA => "QA Engineer",
            _ => role
        };
    }

    /// <summary>Hours per working day for scheduling calculations.</summary>
    public const double HoursPerDay = 8.0;

    /// <summary>Minimum allocation block in hours (half-day).</summary>
    public const double MinAllocationHours = 4.0;

    /// <summary>Audit action types.</summary>
    public static class AuditAction
    {
        public const string Created = "Created";
        public const string Updated = "Updated";
        public const string Deleted = "Deleted";
        public const string SchedulerRun = "SchedulerRun";
        public const string Import = "Import";
    }

    /// <summary>Entity type identifiers for audit logging.</summary>
    public static class EntityType
    {
        public const string Task = "Task";
        public const string Resource = "Resource";
        public const string Adjustment = "Adjustment";
        public const string Holiday = "Holiday";
        public const string Scheduler = "Scheduler";
    }

    /// <summary>Default team name.</summary>
    public const string DefaultTeam = "Delivery";

    /// <summary>Working week configuration values.</summary>
    public static class WorkingWeek
    {
        public const string SunThu = "sun_thu";
        public const string MonFri = "mon_fri";

        /// <summary>
        /// Returns the <see cref="DayOfWeek"/> values that are non-working days
        /// for the given working-week code. Defaults to Sun-Thu (Fri/Sat off).
        /// </summary>
        public static HashSet<DayOfWeek> GetWeekendDays(string workingWeekCode) =>
            workingWeekCode switch
            {
                MonFri => new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
                _ => new HashSet<DayOfWeek> { DayOfWeek.Friday, DayOfWeek.Saturday }   // SunThu default
            };
    }

    /// <summary>Seniority levels for team members.</summary>
    public static class Seniority
    {
        public const string Junior = "Junior";
        public const string Mid = "Mid";
        public const string Senior = "Senior";
        public const string Principal = "Principal";

        public static readonly IReadOnlyList<string> Levels = [Junior, Mid, Senior, Principal];

        public static readonly IReadOnlyDictionary<string, int> Rank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [Junior] = 1, [Mid] = 2, [Senior] = 3, [Principal] = 4
        };

        public static bool IsValid(string level) => Rank.ContainsKey(level);
    }

    /// <summary>Task dependency relationship types.</summary>
    public static class DependencyType
    {
        public const string FinishToStart = "FS";
        public const string StartToStart = "SS";
        public const string FinishToFinish = "FF";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { FinishToStart, StartToStart, FinishToFinish };
        public static bool IsValid(string type) => All.Contains(type);
    }

    /// <summary>Scheduling strategy identifiers.</summary>
    public static class SchedulingStrategy
    {
        public const string PriorityFirst = "priority_first";
        public const string DeadlineFirst = "deadline_first";
        public const string BalancedWorkload = "balanced_workload";
        public const string CriticalPath = "critical_path";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PriorityFirst, DeadlineFirst, BalancedWorkload, CriticalPath
        };
    }
}
