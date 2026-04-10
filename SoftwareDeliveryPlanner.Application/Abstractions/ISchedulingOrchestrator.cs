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

    // Calendar
    Task<List<CalendarDay>> GetCalendarAsync(CancellationToken cancellationToken = default);

    // Timeline
    Task<TimelineDataDto> GetTimelineDataAsync(string resourceId, DateTime start, DateTime end, CancellationToken cancellationToken = default);

    // Output
    Task<List<Dictionary<string, object?>>> GetOutputPlanAsync(CancellationToken cancellationToken = default);
}
