using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Models;

/// <summary>
/// Represents the effort estimation for a single role phase within a task.
/// A task has one or more breakdown entries that define how much work each role must perform.
/// The <see cref="SortOrder"/> is automatically assigned based on the pipeline convention
/// (BA → SA → UX → UI → DEV → QA) defined in <see cref="DomainConstants.ResourceRole.PipelineOrder"/>.
/// </summary>
public class TaskEffortBreakdown
{
    public int Id { get; private set; }
    public string TaskId { get; private set; } = string.Empty;

    /// <summary>Role code (e.g. "DEV", "QA", "BA"). Must match <see cref="DomainConstants.ResourceRole"/> constants.</summary>
    public string Role { get; private set; } = string.Empty;

    /// <summary>Effort estimation in person-days for this role phase.</summary>
    public double EstimationDays { get; private set; }

    /// <summary>
    /// Percentage of overlap allowed with the previous phase (0–100).
    /// 0 = fully sequential (previous phase must finish before this starts).
    /// 20 = this phase can start when the previous phase is 80% complete.
    /// Only meaningful for SortOrder > 1 (first phase always has OverlapPct = 0).
    /// </summary>
    public double OverlapPct { get; private set; }

    /// <summary>
    /// Auto-assigned execution order within the task, based on the role pipeline convention.
    /// Lower numbers execute first.
    /// </summary>
    public int SortOrder { get; private set; }

    // ── Navigation ───────────────────────────────────────────
    public TaskItem? Task { get; private set; }

    private TaskEffortBreakdown() { }

    // ── Domain factory ────────────────────────────────────────────────────────
    /// <summary>
    /// Creates and validates a new effort breakdown entry.
    /// Called internally by <see cref="TaskItem.AddEffortBreakdown"/> or <see cref="TaskItem.Create"/>.
    /// </summary>
    internal static TaskEffortBreakdown Create(
        string taskId,
        string role,
        double estimationDays,
        double overlapPct = 0)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            throw new DomainException("Task ID is required for effort breakdown.");

        if (!DomainConstants.ResourceRole.AllRoles.Contains(role))
            throw new DomainException($"Invalid role '{role}'. Valid roles: {string.Join(", ", DomainConstants.ResourceRole.AllRoles)}.");

        if (estimationDays <= 0)
            throw new DomainException($"Estimation days for role '{role}' must be greater than zero.");

        if (overlapPct < 0 || overlapPct > 100)
            throw new DomainException($"Overlap percentage for role '{role}' must be between 0 and 100.");

        return new TaskEffortBreakdown
        {
            TaskId = taskId.Trim().ToUpperInvariant(),
            Role = role.ToUpperInvariant(),
            EstimationDays = estimationDays,
            OverlapPct = overlapPct,
            SortOrder = DomainConstants.ResourceRole.GetPipelineSortOrder(role)
        };
    }

    /// <summary>Updates estimation and overlap for this breakdown entry.</summary>
    internal void Update(double estimationDays, double overlapPct)
    {
        if (estimationDays <= 0)
            throw new DomainException($"Estimation days for role '{Role}' must be greater than zero.");

        if (overlapPct < 0 || overlapPct > 100)
            throw new DomainException($"Overlap percentage for role '{Role}' must be between 0 and 100.");

        EstimationDays = estimationDays;
        OverlapPct = overlapPct;
    }
}
