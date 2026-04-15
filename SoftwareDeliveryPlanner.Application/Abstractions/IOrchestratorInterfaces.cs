using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Application.Abstractions;

/// <summary>Task CRUD operations.</summary>
public interface ITaskOrchestrator
{
    Task<List<TaskItem>> GetTasksAsync(CancellationToken cancellationToken = default);
    Task<int> GetTaskCountAsync(CancellationToken cancellationToken = default);
    Task UpsertTaskAsync(TaskItem task, bool isNew, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>Resource CRUD operations.</summary>
public interface IResourceOrchestrator
{
    Task<List<TeamMember>> GetResourcesAsync(CancellationToken cancellationToken = default);
    Task<int> GetResourceCountAsync(CancellationToken cancellationToken = default);
    Task UpsertResourceAsync(TeamMember resource, bool isNew, CancellationToken cancellationToken = default);
    Task DeleteResourceAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>Adjustment CRUD operations.</summary>
public interface IAdjustmentOrchestrator
{
    Task<List<Adjustment>> GetAdjustmentsAsync(CancellationToken cancellationToken = default);
    Task AddAdjustmentAsync(Adjustment adjustment, CancellationToken cancellationToken = default);
    Task DeleteAdjustmentAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>Holiday CRUD + overlap checking + copy + working days computation.</summary>
public interface IHolidayOrchestrator
{
    Task<List<Holiday>> GetHolidaysAsync(CancellationToken cancellationToken = default);
    Task UpsertHolidayAsync(Holiday holiday, bool isNew, CancellationToken cancellationToken = default);
    Task DeleteHolidayAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if any existing holiday overlaps the given date range.
    /// Optionally excludes one holiday by ID (for edits).
    /// </summary>
    Task<bool> HasHolidayOverlapAsync(DateTime startDate, DateTime endDate, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies all holidays from the source year to the target year.
    /// Returns the number of holidays copied.
    /// </summary>
    Task<int> CopyHolidaysToYearAsync(int sourceYear, int targetYear, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes the number of working days lost in a date range
    /// (i.e. days that are working days: Sun-Thu).
    /// </summary>
    Task<int> GetHolidayWorkingDaysLostAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}

/// <summary>Scheduler execution and dashboard KPIs.</summary>
public interface ISchedulerService
{
    Task<string> RunSchedulerAsync(CancellationToken cancellationToken = default);
    Task<DashboardKpisDto> GetDashboardKpisAsync(CancellationToken cancellationToken = default);
}

/// <summary>Read-only planning views (calendar, timeline, output).</summary>
public interface IPlanningQueryService
{
    Task<List<CalendarDay>> GetCalendarAsync(CancellationToken cancellationToken = default);
    Task<TimelineDataDto> GetTimelineDataAsync(string resourceId, DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task<List<OutputPlanRowDto>> GetOutputPlanAsync(CancellationToken cancellationToken = default);
}
