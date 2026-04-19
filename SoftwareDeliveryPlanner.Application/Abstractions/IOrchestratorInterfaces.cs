using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Application.Abstractions;

public interface ITaskOrchestrator
{
    Task<List<TaskItem>> GetTasksAsync(CancellationToken cancellationToken = default);
    Task<TaskItem?> GetTaskByTaskIdAsync(string taskId, CancellationToken cancellationToken = default);
    Task<int> GetTaskCountAsync(CancellationToken cancellationToken = default);
    Task UpsertTaskAsync(
        int id, string taskId, string serviceName, double devEstimation,
        double maxDev, int priority, DateTime? strictDate,
        string? dependsOnTaskIds, bool isNew,
        CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(int id, CancellationToken cancellationToken = default);
}

public interface IResourceOrchestrator
{
    Task<List<TeamMember>> GetResourcesAsync(CancellationToken cancellationToken = default);
    Task<int> GetResourceCountAsync(CancellationToken cancellationToken = default);
    Task UpsertResourceAsync(
        int id, string resourceId, string resourceName, string role,
        string team, double availabilityPct, double dailyCapacity,
        DateTime startDate, string active, string? notes, bool isNew,
        CancellationToken cancellationToken = default);
    Task DeleteResourceAsync(int id, CancellationToken cancellationToken = default);
}

public interface IRoleOrchestrator
{
    Task<List<Role>> GetRolesAsync(bool includeInactive = true, CancellationToken cancellationToken = default);
    Task UpsertRoleAsync(
        int id,
        string code,
        string displayName,
        bool isActive,
        int sortOrder,
        bool isNew,
        CancellationToken cancellationToken = default);
    Task DeleteRoleAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> RoleCodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsRoleInUseAsync(string code, CancellationToken cancellationToken = default);
}

public interface IAdjustmentOrchestrator
{
    Task<List<Adjustment>> GetAdjustmentsAsync(CancellationToken cancellationToken = default);
    Task AddAdjustmentAsync(
        string resourceId, string adjType, double availabilityPct,
        DateTime adjStart, DateTime adjEnd, string? notes,
        CancellationToken cancellationToken = default);
    Task DeleteAdjustmentAsync(int id, CancellationToken cancellationToken = default);
}

public interface IHolidayOrchestrator
{
    Task<List<Holiday>> GetHolidaysAsync(CancellationToken cancellationToken = default);
    Task UpsertHolidayAsync(
        int id, string holidayName, DateTime startDate, DateTime endDate,
        string holidayType, string? notes, bool isNew,
        CancellationToken cancellationToken = default);
    Task DeleteHolidayAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> HasHolidayOverlapAsync(DateTime startDate, DateTime endDate, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<int> CopyHolidaysToYearAsync(int sourceYear, int targetYear, CancellationToken cancellationToken = default);
    Task<int> GetHolidayWorkingDaysLostAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}

public interface ITaskNoteOrchestrator
{
    Task<List<TaskNote>> GetNotesAsync(string taskId);
    Task AddNoteAsync(TaskNote note);
    Task DeleteNoteAsync(int id);
}

public interface ISchedulerService
{
    Task<string> RunSchedulerAsync(CancellationToken cancellationToken = default);
    Task<DashboardKpisDto> GetDashboardKpisAsync(CancellationToken cancellationToken = default);
}

public interface INotificationOrchestrator
{
    Task<List<RiskNotification>> GetNotificationsAsync(bool unreadOnly);
    Task MarkAllAsReadAsync();
}

public interface IAuditService
{
    Task LogAsync(string action, string entityType, string entityId, string description, string? oldValue = null, string? newValue = null);
    Task<List<AuditEntry>> GetRecentAsync(int count = 50);
}

public interface IScenarioOrchestrator
{
    Task<List<PlanScenario>> GetScenariosAsync();
    Task<PlanScenario?> GetScenarioWithSnapshotsAsync(int id);
    Task SaveScenarioAsync(PlanScenario scenario);
    Task SaveScenarioWithSnapshotsAsync(PlanScenario scenario, List<TaskItem> tasks);
    Task DeleteScenarioAsync(int id);
}

public interface IPlanningQueryService
{
    Task<List<CalendarDay>> GetCalendarAsync(CancellationToken cancellationToken = default);
    Task<TimelineDataDto> GetTimelineDataAsync(string resourceId, DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task<List<OutputPlanRowDto>> GetOutputPlanAsync(CancellationToken cancellationToken = default);
    Task<TaskTimelineDto> GetTaskTimelineAsync(string taskId, CancellationToken cancellationToken = default);
    Task<Planning.Queries.WorkloadHeatmapDto> GetWorkloadHeatmapAsync(CancellationToken cancellationToken = default);
    Task<List<Planning.Queries.RiskTrendPointDto>> GetRiskTrendAsync(int maxPoints);
    Task<List<Tasks.Queries.TaskAllocationDto>> GetTaskAllocationsAsync(string taskId, CancellationToken cancellationToken = default);
    Task<DateTime?> GetLastSchedulerRunAsync(CancellationToken cancellationToken = default);
}
