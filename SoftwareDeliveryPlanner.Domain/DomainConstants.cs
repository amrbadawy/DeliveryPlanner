namespace SoftwareDeliveryPlanner.Domain;

/// <summary>
/// Centralizes magic strings used across the domain, application, and infrastructure layers.
/// Values here must match the reference data seeded in the database.
/// </summary>
public static class DomainConstants
{
    /// <summary>Task scheduling status values.</summary>
    public static class TaskStatus
    {
        public const string NotStarted = "NOT_STARTED";
        public const string InProgress = "IN_PROGRESS";
        public const string Completed = "COMPLETED";

        private static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
            { NotStarted, InProgress, Completed };

        public static bool IsValid(string status) => All.Contains(status);
    }

    /// <summary>Delivery risk assessment labels.</summary>
    public static class DeliveryRisk
    {
        public const string OnTrack = "ON_TRACK";
        public const string AtRisk = "AT_RISK";
        public const string Late = "LATE";

        private static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
            { OnTrack, AtRisk, Late };

        public static bool IsValid(string risk) => All.Contains(risk);
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
        public const string WeekNumbering = "week_numbering";
        public const string GanttZoomLevel = "gantt_zoom_level";
    }

    /// <summary>Holiday classification types.</summary>
    public static class HolidayType
    {
        public const string National = "NATIONAL";
        public const string Religious = "RELIGIOUS";
        public const string Company = "COMPANY";
    }

    /// <summary>Adjustment classification types.</summary>
    public static class AdjustmentType
    {
        public const string Vacation = "VACATION";
        public const string Training = "TRAINING";
        public const string SickLeave = "SICK_LEAVE";
        public const string Other = "OTHER";
    }

    /// <summary>Active/inactive status for resources.</summary>
    public static class ActiveStatus
    {
        public const string Yes = "YES";
        public const string No = "NO";
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
        public static int GetPipelineSortOrder(string role)
        {
            for (int i = 0; i < PipelineOrder.Count; i++)
                if (string.Equals(PipelineOrder[i], role, StringComparison.OrdinalIgnoreCase))
                    return i + 1;
            return int.MaxValue;
        }

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
        public const string SunThu = "SUN_THU";
        public const string MonFri = "MON_FRI";

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

        /// <summary>
        /// Returns the first day of the working week for the given code.
        /// Sun-Thu → Sunday, Mon-Fri → Monday.
        /// </summary>
        public static DayOfWeek GetWeekStartDay(string workingWeekCode) =>
            workingWeekCode switch
            {
                MonFri => DayOfWeek.Monday,
                _ => DayOfWeek.Sunday  // SunThu default
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

    /// <summary>
    /// Week-numbering rule used when rendering calendar week labels (e.g. on the Gantt header).
    /// Centralized so all screens (Gantt, ScenarioGantt, Heatmap, UtilizationForecast) stay consistent.
    /// </summary>
    public static class WeekNumbering
    {
        /// <summary>ISO 8601 — Monday-start, FirstFourDayWeek rule. Standard in Europe and most of the world.</summary>
        public const string Iso8601 = "ISO_8601";

        /// <summary>Sunday-start, FirstDay rule (week containing Jan 1 is week 1). Common in Arab countries and the US.</summary>
        public const string SundayFirstDay = "SUNDAY_FIRSTDAY";

        /// <summary>Monday-start, FirstDay rule.</summary>
        public const string MondayFirstDay = "MONDAY_FIRSTDAY";

        /// <summary>Derive numbering from the configured WorkingWeek setting (SUN_THU → Sunday, MON_FRI → ISO).</summary>
        public const string FollowWorkingWeek = "FOLLOW_WORKING_WEEK";

        public const string Default = FollowWorkingWeek;

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Iso8601, SundayFirstDay, MondayFirstDay, FollowWorkingWeek
        };

        public static bool IsValid(string? code) => code is not null && All.Contains(code);

        public static string DisplayName(string code) => code switch
        {
            Iso8601 => "ISO 8601 (Monday-start)",
            SundayFirstDay => "Sunday-start (FirstDay rule)",
            MondayFirstDay => "Monday-start (FirstDay rule)",
            FollowWorkingWeek => "Follow Working Week setting",
            _ => code
        };
    }

    /// <summary>
    /// Default zoom level for the Gantt timeline. Persisted in Settings so the user's choice
    /// survives reloads and is shared across Gantt and ScenarioGantt screens.
    /// </summary>
    public static class GanttZoomLevel
    {
        public const string Day = "DAY";
        public const string Week = "WEEK";
        public const string Month = "MONTH";
        public const string Quarter = "QUARTER";

        public const string Default = Week;

        public static readonly IReadOnlyList<string> Levels = new[] { Day, Week, Month, Quarter };

        public static bool IsValid(string? code) => code is not null && Levels.Contains(code, StringComparer.OrdinalIgnoreCase);

        public static string DisplayName(string code) => code switch
        {
            Day => "Day",
            Week => "Week",
            Month => "Month",
            Quarter => "Quarter",
            _ => code
        };

        /// <summary>Pixel-per-day density used to compute timeline min-width per zoom level.</summary>
        public static double PixelsPerDay(string code) => code switch
        {
            Day => 32.0,
            Week => 80.0 / 7.0,
            Month => 120.0 / 30.0,
            Quarter => 160.0 / 90.0,
            _ => 80.0 / 7.0
        };

        /// <summary>Picks an adaptive default zoom level based on plan length. Used on first visit only.</summary>
        public static string AdaptiveDefault(int totalDays) => totalDays switch
        {
            <= 21 => Day,
            <= 180 => Week,
            <= 540 => Month,
            _ => Quarter
        };
    }
}
