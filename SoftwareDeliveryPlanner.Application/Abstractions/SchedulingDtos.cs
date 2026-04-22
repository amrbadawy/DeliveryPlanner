namespace SoftwareDeliveryPlanner.Application.Abstractions;

/// <summary>Result of a feasibility check for a task with a strict deadline.</summary>
public sealed record FeasibilityResultDto(
    string TaskId,
    string ServiceName,
    bool IsFeasible,
    DateTime? EarliestFinish,
    int SlackDays,
    string? Bottleneck,
    string? Recommendation);

/// <summary>Resource overallocation alert for a specific day.</summary>
public sealed record OverallocationAlertDto(
    string ResourceId,
    string ResourceName,
    DateTime Date,
    double AllocatedHours,
    double CapacityHours,
    double OverageHours,
    List<string> TaskIds,
    string Severity);

/// <summary>Preview of what would change if the scheduler runs.</summary>
public sealed record ScheduleDiffDto(
    List<TaskDiffEntry> TaskChanges,
    int TasksAffected,
    int TasksUnchanged,
    int NewAllocations);

public sealed record TaskDiffEntry(
    string TaskId,
    string TaskName,
    DateTime? OldStart, DateTime? NewStart,
    DateTime? OldFinish, DateTime? NewFinish,
    string? OldRisk, string? NewRisk,
    string? OldAssignedResources, string? NewAssignedResources,
    string ChangeType);

/// <summary>Utilization forecast for a single week.</summary>
public sealed record ForecastWeekDto(
    DateTime WeekStart,
    double TotalCapacityHours,
    double AllocatedHours,
    double UtilizationPct,
    double FreeHours,
    int OverallocatedResources);

/// <summary>Complete utilization forecast response.</summary>
public sealed record UtilizationForecastDto(List<ForecastWeekDto> Weeks);
