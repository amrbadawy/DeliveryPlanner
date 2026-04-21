using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Events;
using SoftwareDeliveryPlanner.SharedKernel;
using SoftwareDeliveryPlanner.SharedKernel.ValueObjects;
using TaskIdVO = SoftwareDeliveryPlanner.SharedKernel.ValueObjects.TaskId;

namespace SoftwareDeliveryPlanner.Domain.Models;

public class TaskItem : AggregateRoot
{
    private readonly List<TaskEffortBreakdown> _effortBreakdown = [];

    public int Id { get; private set; }
    public string TaskId { get; private set; } = string.Empty;
    public string ServiceName { get; private set; } = string.Empty;
    public double MaxResource { get; private set; } = 1.0;
    public DateTime? StrictDate { get; private set; }
    public int Priority { get; private set; } = 5;
    public int? SchedulingRank { get; private set; }
    public double? AssignedResource { get; private set; }
    public string? AssignedResourceId { get; private set; }
    public DateTime? PlannedStart { get; private set; }
    public DateTime? PlannedFinish { get; private set; }
    public int? Duration { get; private set; }
    public string Status { get; private set; } = DomainConstants.TaskStatus.NotStarted;
    public string DeliveryRisk { get; private set; } = DomainConstants.DeliveryRisk.OnTrack;
    public DateTime? OverrideStart { get; private set; }
    public string? DependsOnTaskIds { get; private set; }
    public string? Comments { get; private set; }
    public string? Phase { get; private set; }
    public string? PreferredResourceIds { get; private set; }
    public DateTime CreatedAt { get; private set; } = TimeProvider.System.GetLocalNow().DateTime;
    public DateTime UpdatedAt { get; private set; } = TimeProvider.System.GetLocalNow().DateTime;

    /// <summary>Effort breakdown by role. Managed via <see cref="SetEffortBreakdown"/>.</summary>
    public IReadOnlyCollection<TaskEffortBreakdown> EffortBreakdown => _effortBreakdown.AsReadOnly();

    /// <summary>Total estimation in person-days (sum of all role phases).</summary>
    public double TotalEstimationDays => _effortBreakdown.Sum(e => e.EstimationDays);

    // ── Domain factory ────────────────────────────────────────────────────────
    /// <summary>
    /// Creates and validates a new <see cref="TaskItem"/> using domain invariants.
    /// Raises <see cref="DomainException"/> on any violation.
    /// </summary>
    public static TaskItem Create(
        string taskId,
        string serviceName,
        double maxResource,
        int priority,
        List<(string Role, double EstimationDays, double OverlapPct)> effortBreakdown,
        DateTime? strictDate = null,
        string? dependsOnTaskIds = null,
        DateTime? overrideStart = null,
        string? phase = null,
        string? preferredResourceIds = null)
    {
        if (!TaskIdVO.TryCreate(taskId, out _))
            throw new DomainException($"Invalid Task ID '{taskId}'. Expected format: AAA-000.");

        if (string.IsNullOrWhiteSpace(serviceName))
            throw new DomainException("Service name must not be empty.");

        if (maxResource <= 0)
            throw new DomainException("Max resources must be greater than zero.");

        if (priority < 1 || priority > 10)
            throw new DomainException("Priority must be between 1 and 10.");

        var normalizedId = taskId.Trim().ToUpperInvariant();

        var task = new TaskItem
        {
            TaskId = normalizedId,
            ServiceName = serviceName.Trim(),
            MaxResource = maxResource,
            Priority = priority,
            StrictDate = strictDate,
            DependsOnTaskIds = string.IsNullOrWhiteSpace(dependsOnTaskIds) ? null : dependsOnTaskIds.Trim(),
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
        double maxResource,
        int priority,
        List<(string Role, double EstimationDays, double OverlapPct)> effortBreakdown,
        DateTime? strictDate = null,
        string? dependsOnTaskIds = null,
        string? phase = null,
        string? preferredResourceIds = null)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new DomainException("Service name must not be empty.");

        if (maxResource <= 0)
            throw new DomainException("Max resources must be greater than zero.");

        if (priority < 1 || priority > 10)
            throw new DomainException("Priority must be between 1 and 10.");

        ServiceName = serviceName.Trim();
        MaxResource = maxResource;
        Priority = priority;
        StrictDate = strictDate;
        DependsOnTaskIds = string.IsNullOrWhiteSpace(dependsOnTaskIds) ? null : dependsOnTaskIds.Trim();
        Phase = string.IsNullOrWhiteSpace(phase) ? null : phase.Trim();
        PreferredResourceIds = string.IsNullOrWhiteSpace(preferredResourceIds) ? null : preferredResourceIds.Trim();
        UpdatedAt = TimeProvider.System.GetLocalNow().DateTime;

        SetEffortBreakdownInternal(TaskId, effortBreakdown);
        RaiseDomainEvent(new TaskUpdatedEvent(TaskId));
    }

    // ── Effort breakdown management ───────────────────────────────────────────
    private void SetEffortBreakdownInternal(
        string taskId,
        List<(string Role, double EstimationDays, double OverlapPct)> breakdown)
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
        foreach (var (role, days, overlap) in breakdown)
        {
            _effortBreakdown.Add(TaskEffortBreakdown.Create(taskId, role, days, overlap));
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
        double assignedResource,
        DateTime? plannedStart,
        DateTime? plannedFinish,
        int duration,
        string status,
        string deliveryRisk,
        string? assignedResourceId = null)
    {
        AssignedResource = assignedResource;
        PlannedStart = plannedStart;
        PlannedFinish = plannedFinish;
        Duration = duration;
        Status = status;
        DeliveryRisk = deliveryRisk;
        AssignedResourceId = assignedResourceId;
        UpdatedAt = TimeProvider.System.GetLocalNow().DateTime;
    }
}
