using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Application.Abstractions;

public interface ISchedulingOrchestrator
{
    Task<string> RunSchedulerAsync(CancellationToken cancellationToken = default);
    Task<DashboardKpisDto> GetDashboardKpisAsync(CancellationToken cancellationToken = default);

    // Tasks
    Task<List<TaskItem>> GetTasksAsync(CancellationToken cancellationToken = default);
    Task<int> GetTaskCountAsync(CancellationToken cancellationToken = default);
    Task UpsertTaskAsync(TaskItem task, bool isNew, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(int id, CancellationToken cancellationToken = default);

    // Resources
    Task<List<TeamMember>> GetResourcesAsync(CancellationToken cancellationToken = default);
    Task<int> GetResourceCountAsync(CancellationToken cancellationToken = default);
    Task UpsertResourceAsync(TeamMember resource, bool isNew, CancellationToken cancellationToken = default);
    Task DeleteResourceAsync(int id, CancellationToken cancellationToken = default);

    // Adjustments
    Task<List<Adjustment>> GetAdjustmentsAsync(CancellationToken cancellationToken = default);
    Task AddAdjustmentAsync(Adjustment adjustment, CancellationToken cancellationToken = default);
    Task DeleteAdjustmentAsync(int id, CancellationToken cancellationToken = default);

    // Holidays
    Task<List<Holiday>> GetHolidaysAsync(CancellationToken cancellationToken = default);
    Task UpsertHolidayAsync(Holiday holiday, bool isNew, CancellationToken cancellationToken = default);
    Task DeleteHolidayAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if any existing holiday overlaps the given date range.
    /// Optionally excludes one holiday by ID (for edits).
    /// Overlap formula: A.StartDate &lt;= B.EndDate AND A.EndDate &gt;= B.StartDate
    /// </summary>
    Task<bool> HasHolidayOverlapAsync(DateTime startDate, DateTime endDate, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies all holidays from the source year to the target year, shifting dates by the year delta.
    /// Returns the number of holidays copied.
    /// </summary>
    Task<int> CopyHolidaysToYearAsync(int sourceYear, int targetYear, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes the number of working days lost in a date range
    /// (i.e. days in range that are working days — Sun-Thu and not weekend).
    /// </summary>
    Task<int> GetHolidayWorkingDaysLostAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    // Calendar
    Task<List<CalendarDay>> GetCalendarAsync(CancellationToken cancellationToken = default);

    // Timeline
    Task<TimelineDataDto> GetTimelineDataAsync(string resourceId, DateTime start, DateTime end, CancellationToken cancellationToken = default);

    // Output
    Task<List<Dictionary<string, object?>>> GetOutputPlanAsync(CancellationToken cancellationToken = default);
}
