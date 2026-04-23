using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Events;
using SoftwareDeliveryPlanner.SharedKernel;
using SoftwareDeliveryPlanner.SharedKernel.ValueObjects;
using TaskIdVO = SoftwareDeliveryPlanner.SharedKernel.ValueObjects.TaskId;

namespace SoftwareDeliveryPlanner.Domain.Models;

public class TaskItem : AggregateRoot
{
    private readonly List<TaskEffortBreakdown> _effortBreakdown = [];
    private readonly List<TaskDependency> _dependencies = [];

    public int Id { get; private set; }
    public string TaskId { get; private set; } = string.Empty;
    public string ServiceName { get; private set; } = string.Empty;
    public DateTime? StrictDate { get; private set; }
    public int Priority { get; private set; } = 5;
    public int? SchedulingRank { get; private set; }
    public double? PeakConcurrency { get; private set; }
    public string? AssignedResourceId { get; private set; }
    public DateTime? PlannedStart { get; private set; }
    public DateTime? PlannedFinish { get; private set; }
    public int? Duration { get; private set; }
    public string Status { get; private set; } = DomainConstants.TaskStatus.NotStarted;
    public string DeliveryRisk { get; private set; } = DomainConstants.DeliveryRisk.OnTrack;
    public DateTime? OverrideStart { get; private set; }
    public string? Comments { get; private set; }
    public string? Phase { get; private set; }
    public string? PreferredResourceIds { get; private set; }
    public DateTime CreatedAt { get; private set; } = TimeProvider.System.GetLocalNow().DateTime;
    public DateTime UpdatedAt { get; private set; } = TimeProvider.System.GetLocalNow().DateTime;

    /// <summary>Effort breakdown by role. Managed via <see cref="SetEffortBreakdown"/>.</summary>
    public IReadOnlyCollection<TaskEffortBreakdown> EffortBreakdown => _effortBreakdown.AsReadOnly();

    /// <summary>Total estimation in person-days (sum of all role phases).</summary>
    public double TotalEstimationDays => _effortBreakdown.Sum(e => e.EstimationDays);

    /// <summary>Task dependencies (predecessor relationships).</summary>
    public IReadOnlyCollection<TaskDependency> Dependencies => _dependencies.AsReadOnly();

    /// <summary>Comma-separated predecessor task IDs for display/export.</summary>
    public string? DependsOnTaskIds => _dependencies.Count > 0
        ? string.Join(",", _dependencies.Select(d => d.PredecessorTaskId).OrderBy(id => id))
        : null;

    // ── Domain factory ────────────────────────────────────────────────────────
    /// <summary>
    /// Creates and validates a new <see cref="TaskItem"/> using domain invariants.
    /// Raises <see cref="DomainException"/> on any violation.
    /// </summary>
    public static TaskItem Create(
        string taskId,
        string serviceName,
        int priority,
        List<EffortBreakdownSpec> effortBreakdown,
        DateTime? strictDate = null,
        DateTime? overrideStart = null,
        string? phase = null,
        string? preferredResourceIds = null)
    {
        if (!TaskIdVO.TryCreate(taskId, out _))
            throw new DomainException($"Invalid Task ID '{taskId}'. Expected format: AAA-000.");

        if (string.IsNullOrWhiteSpace(serviceName))
            throw new DomainException("Service name must not be empty.");

        if (priority < 1 || priority > 10)
            throw new DomainException("Priority must be between 1 and 10.");

        var normalizedId = taskId.Trim().ToUpperInvariant();

        var task = new TaskItem
        {
            TaskId = normalizedId,
            ServiceName = serviceName.Trim(),
            Priority = priority,
            StrictDate = strictDate,
            OverrideStart = overrideStart,
            Phase = string.IsNullOrWhiteSpace(phase) ? null : phase.Trim(),
            PreferredResourceIds = string.IsNullOrWhiteSpace(preferredResourceIds) ? null : preferredResourceIds.Trim()
        };

        task.SetEffortBreakdownInternal(normalizedId, effortBreakdown);
        task.RaiseDomainEvent(new TaskCreatedEvent(task.TaskId, task.ServiceName));
        return task;
    }

    // ── Domain mutation ───────────────────────────────────────────────────────
    /// <summary>
    /// Updates user-editable properties and raises <see cref="TaskUpdatedEvent"/>.
    /// </summary>
    public void Update(
        string serviceName,
        int priority,
        List<EffortBreakdownSpec> effortBreakdown,
        DateTime? strictDate = null,
        string? phase = null,
        string? preferredResourceIds = null)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new DomainException("Service name must not be empty.");

        if (priority < 1 || priority > 10)
            throw new DomainException("Priority must be between 1 and 10.");

        ServiceName = serviceName.Trim();
        Priority = priority;
        StrictDate = strictDate;
        Phase = string.IsNullOrWhiteSpace(phase) ? null : phase.Trim();
        PreferredResourceIds = string.IsNullOrWhiteSpace(preferredResourceIds) ? null : preferredResourceIds.Trim();
        UpdatedAt = TimeProvider.System.GetLocalNow().DateTime;

        SetEffortBreakdownInternal(TaskId, effortBreakdown);
        RaiseDomainEvent(new TaskUpdatedEvent(TaskId));
    }

    // ── Dependency management ─────────────────────────────────────────────────
    /// <summary>
    /// Adds a dependency on a predecessor task with the specified relationship type.
    /// </summary>
    public TaskDependency AddDependency(string predecessorTaskId, string type = DomainConstants.DependencyType.FinishToStart, int lagDays = 0, double overlapPct = 0)
    {
        if (string.IsNullOrWhiteSpace(predecessorTaskId))
            throw new DomainException("Predecessor task ID is required.");

        var normalizedPredId = predecessorTaskId.Trim().ToUpperInvariant();
        if (normalizedPredId.Equals(TaskId, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("A task cannot depend on itself.");

        if (_dependencies.Any(d => d.PredecessorTaskId.Equals(normalizedPredId, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Dependency on '{normalizedPredId}' already exists.");

        var dep = TaskDependency.Create(TaskId, normalizedPredId, type, lagDays, overlapPct);
        _dependencies.Add(dep);
        return dep;
    }

    /// <summary>Removes a dependency on the specified predecessor task.</summary>
    public void RemoveDependency(string predecessorTaskId)
    {
        var dep = _dependencies.FirstOrDefault(d => d.PredecessorTaskId.Equals(predecessorTaskId, StringComparison.OrdinalIgnoreCase))
            ?? throw new DomainException($"Dependency on '{predecessorTaskId}' not found.");
        _dependencies.Remove(dep);
    }

    /// <summary>Removes all dependencies from this task.</summary>
    public void ClearDependencies() => _dependencies.Clear();

    // ── Effort breakdown management ───────────────────────────────────────────
    private void SetEffortBreakdownInternal(
        string taskId,
        List<EffortBreakdownSpec> breakdown)
    {
        if (breakdown is null || breakdown.Count == 0)
            throw new DomainException("At least DEV and QA effort breakdown entries are required.");

        // Validate required roles
        var roles = breakdown.Select(b => b.Role.ToUpperInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var required in DomainConstants.ResourceRole.RequiredRoles)
        {
            if (!roles.Contains(required))
                throw new DomainException($"Effort breakdown must include the required role '{required}'.");
        }

        // Check for duplicates
        if (roles.Count != breakdown.Count)
            throw new DomainException("Duplicate roles are not allowed in effort breakdown.");

        // Validate all roles are known
        foreach (var role in roles)
        {
            if (!DomainConstants.ResourceRole.AllRoles.Contains(role))
                throw new DomainException($"Invalid role '{role}' in effort breakdown.");
        }

        _effortBreakdown.Clear();
        foreach (var spec in breakdown)
        {
            _effortBreakdown.Add(TaskEffortBreakdown.Create(taskId, spec.Role, spec.EstimationDays, spec.OverlapPct, spec.MaxFte, spec.MinSeniority));
        }

        // Sort by pipeline order
        _effortBreakdown.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));

        // First phase always has 0% overlap
        if (_effortBreakdown.Count > 0 && _effortBreakdown[0].OverlapPct != 0)
        {
            _effortBreakdown[0].Update(_effortBreakdown[0].EstimationDays, 0);
        }
    }

    // ── Scheduling engine methods ─────────────────────────────────────────────
    /// <summary>
    /// Applies the scheduling rank computed by the scheduling engine.
    /// </summary>
    public void ApplySchedulingRank(int rank)
    {
        SchedulingRank = rank;
    }

    /// <summary>
    /// Applies scheduling results computed by the scheduling engine.
    /// Called after the allocation pass to set planned dates, assignment, and status.
    /// Parameters are nullable because tasks with no allocations have no planned dates.
    /// </summary>
    public void ApplySchedulingResult(
        double peakConcurrency,
        DateTime? plannedStart,
        DateTime? plannedFinish,
        int duration,
        string status,
        string deliveryRisk,
        string? assignedResourceId = null)
    {
        PeakConcurrency = peakConcurrency;
        PlannedStart = plannedStart;
        PlannedFinish = plannedFinish;
        Duration = duration;
        Status = status;
        DeliveryRisk = deliveryRisk;
        AssignedResourceId = assignedResourceId;
        UpdatedAt = TimeProvider.System.GetLocalNow().DateTime;
    }
}
