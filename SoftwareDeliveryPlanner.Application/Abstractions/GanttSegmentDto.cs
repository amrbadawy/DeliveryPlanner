namespace SoftwareDeliveryPlanner.Application.Abstractions;

/// <summary>
/// A single role-phase segment within a task's Gantt bar.
/// Derived from allocations grouped by (TaskId, Role).
/// </summary>
public sealed record GanttRoleSegmentDto
{
    public string TaskId { get; }
    public string Role { get; }
    public DateTime SegmentStart { get; }
    public DateTime SegmentEnd { get; }
    public int DurationDays { get; }
    public double MaxFte { get; }
    public IReadOnlyList<GanttSegmentResourceDto> AssignedResources { get; }
    public bool IsEstimated { get; }

    public GanttRoleSegmentDto(
        string TaskId,
        string Role,
        DateTime SegmentStart,
        DateTime SegmentEnd,
        int DurationDays,
        double MaxFte,
        IReadOnlyList<GanttSegmentResourceDto> AssignedResources,
        bool IsEstimated = false)
    {
        if (string.IsNullOrWhiteSpace(TaskId))
            throw new ArgumentException("TaskId is required.", nameof(TaskId));
        if (string.IsNullOrWhiteSpace(Role))
            throw new ArgumentException("Role is required.", nameof(Role));

        this.TaskId = TaskId;
        this.Role = Role;
        this.SegmentStart = SegmentStart;
        this.SegmentEnd = SegmentEnd;
        this.DurationDays = DurationDays;
        this.MaxFte = MaxFte;
        this.AssignedResources = AssignedResources ?? Array.Empty<GanttSegmentResourceDto>();
        this.IsEstimated = IsEstimated;
    }
}

/// <summary>Resource assigned to a role segment.</summary>
public sealed record GanttSegmentResourceDto(
    string ResourceId,
    string ResourceName);

/// <summary>
/// All role segments for a single task, used by Gantt rendering.
/// Immutable after construction — segments are fully built before the DTO is created.
/// </summary>
public sealed record TaskGanttSegmentsDto(
    string TaskId,
    IReadOnlyList<GanttRoleSegmentDto> Segments);
