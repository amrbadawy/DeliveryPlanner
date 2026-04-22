using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Models;

/// <summary>
/// Represents a dependency relationship between two tasks.
/// The dependent task (TaskId) requires the predecessor (PredecessorTaskId) to meet certain conditions before it can start/finish.
/// </summary>
public class TaskDependency
{
    public int Id { get; private set; }
    public string TaskId { get; private set; } = string.Empty;
    public string PredecessorTaskId { get; private set; } = string.Empty;

    /// <summary>Dependency type: FS (Finish-to-Start), SS (Start-to-Start), FF (Finish-to-Finish).</summary>
    public string Type { get; private set; } = DomainConstants.DependencyType.FinishToStart;

    /// <summary>Extra buffer working days after the dependency condition is met.</summary>
    public int LagDays { get; private set; }

    /// <summary>
    /// Percentage overlap allowed (0-100). For FS: 20 means dependent can start when predecessor is 80% complete.
    /// For SS: ignored (start-to-start is immediate + lag). For FF: ignored.
    /// </summary>
    public double OverlapPct { get; private set; }

    // Navigation
    public TaskItem? Task { get; private set; }

    private TaskDependency() { }

    internal static TaskDependency Create(string taskId, string predecessorTaskId, string type, int lagDays, double overlapPct)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            throw new DomainException("Task ID is required for dependency.");

        if (string.IsNullOrWhiteSpace(predecessorTaskId))
            throw new DomainException("Predecessor task ID is required.");

        if (!DomainConstants.DependencyType.IsValid(type))
            throw new DomainException($"Invalid dependency type '{type}'. Valid types: {string.Join(", ", DomainConstants.DependencyType.All)}.");

        if (lagDays < 0)
            throw new DomainException("Lag days cannot be negative.");

        if (overlapPct < 0 || overlapPct > 100)
            throw new DomainException("Overlap percentage must be between 0 and 100.");

        return new TaskDependency
        {
            TaskId = taskId.Trim().ToUpperInvariant(),
            PredecessorTaskId = predecessorTaskId.Trim().ToUpperInvariant(),
            Type = type.ToUpperInvariant(),
            LagDays = lagDays,
            OverlapPct = overlapPct
        };
    }

    /// <summary>Updates dependency type, lag days, and overlap percentage.</summary>
    public void Update(string type, int lagDays, double overlapPct)
    {
        if (!DomainConstants.DependencyType.IsValid(type))
            throw new DomainException($"Invalid dependency type '{type}'.");
        if (lagDays < 0)
            throw new DomainException("Lag days cannot be negative.");
        if (overlapPct < 0 || overlapPct > 100)
            throw new DomainException("Overlap percentage must be between 0 and 100.");

        Type = type.ToUpperInvariant();
        LagDays = lagDays;
        OverlapPct = overlapPct;
    }
}
