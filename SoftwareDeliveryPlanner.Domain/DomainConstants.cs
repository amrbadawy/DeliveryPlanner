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
    }

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
}
