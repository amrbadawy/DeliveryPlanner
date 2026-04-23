namespace SoftwareDeliveryPlanner.Application.Abstractions;

/// <summary>
/// A single role-phase segment within a task's Gantt bar.
/// Derived from allocations grouped by (TaskId, Role).
/// </summary>
public sealed record GanttRoleSegmentDto(
    string TaskId,
    string Role,
    DateTime SegmentStart,
    DateTime SegmentEnd,
    int DurationDays,
    double MaxFte,
    List<GanttSegmentResourceDto> AssignedResources,
    bool IsEstimated = false);

/// <summary>Resource assigned to a role segment.</summary>
public sealed record GanttSegmentResourceDto(
    string ResourceId,
    string ResourceName);

/// <summary>
/// All role segments for a single task, used by Gantt rendering.
/// </summary>
public sealed record TaskGanttSegmentsDto(
    string TaskId,
    List<GanttRoleSegmentDto> Segments);
