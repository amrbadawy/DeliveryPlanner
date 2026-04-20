using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Events;
using SoftwareDeliveryPlanner.SharedKernel;
using SoftwareDeliveryPlanner.SharedKernel.ValueObjects;
using TaskIdVO = SoftwareDeliveryPlanner.SharedKernel.ValueObjects.TaskId;

namespace SoftwareDeliveryPlanner.Domain.Models;

public class TaskItem : AggregateRoot
{
    public int Id { get; private set; }
    public string TaskId { get; private set; } = string.Empty;
    public string ServiceName { get; private set; } = string.Empty;
    public double DevEstimation { get; private set; }
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
    public double? OverrideResource { get; private set; }
    public string? DependsOnTaskIds { get; private set; }
    public string? Comments { get; private set; }
    public DateTime CreatedAt { get; private set; } = TimeProvider.System.GetLocalNow().DateTime;
    public DateTime UpdatedAt { get; private set; } = TimeProvider.System.GetLocalNow().DateTime;

    // ── Domain factory ────────────────────────────────────────────────────────
    /// <summary>
    /// Creates and validates a new <see cref="TaskItem"/> using domain invariants.
    /// Raises <see cref="DomainException"/> on any violation.
    /// </summary>
    public static TaskItem Create(
        string taskId,
        string serviceName,
        double devEstimation,
        double maxResource,
        int priority,
        DateTime? strictDate = null,
        string? dependsOnTaskIds = null,
        DateTime? overrideStart = null,
        double? overrideResource = null)
    {
        if (!TaskIdVO.TryCreate(taskId, out _))
            throw new DomainException($"Invalid Task ID '{taskId}'. Expected format: AAA-000.");

        if (string.IsNullOrWhiteSpace(serviceName))
            throw new DomainException("Service name must not be empty.");

        if (devEstimation < 0)
            throw new DomainException("Dev estimation must not be negative.");

        if (maxResource <= 0)
            throw new DomainException("Max resources must be greater than zero.");

        if (priority < 1 || priority > 10)
            throw new DomainException("Priority must be between 1 and 10.");

        var task = new TaskItem
        {
            TaskId = taskId.Trim().ToUpperInvariant(),
            ServiceName = serviceName.Trim(),
            DevEstimation = devEstimation,
            MaxResource = maxResource,
            Priority = priority,
            StrictDate = strictDate,
            DependsOnTaskIds = string.IsNullOrWhiteSpace(dependsOnTaskIds) ? null : dependsOnTaskIds.Trim(),
            OverrideStart = overrideStart,
            OverrideResource = overrideResource
        };

        task.RaiseDomainEvent(new TaskCreatedEvent(task.TaskId, task.ServiceName));
        return task;
    }

    // ── Domain mutation ───────────────────────────────────────────────────────
    /// <summary>
    /// Updates user-editable properties and raises <see cref="TaskUpdatedEvent"/>.
    /// </summary>
    public void Update(
        string serviceName,
        double devEstimation,
        double maxResource,
        int priority,
        DateTime? strictDate = null,
        string? dependsOnTaskIds = null)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new DomainException("Service name must not be empty.");

        if (devEstimation < 0)
            throw new DomainException("Dev estimation must not be negative.");

        if (maxResource <= 0)
            throw new DomainException("Max resources must be greater than zero.");

        if (priority < 1 || priority > 10)
            throw new DomainException("Priority must be between 1 and 10.");

        ServiceName = serviceName.Trim();
        DevEstimation = devEstimation;
        MaxResource = maxResource;
        Priority = priority;
        StrictDate = strictDate;
        DependsOnTaskIds = string.IsNullOrWhiteSpace(dependsOnTaskIds) ? null : dependsOnTaskIds.Trim();
        UpdatedAt = TimeProvider.System.GetLocalNow().DateTime;

        RaiseDomainEvent(new TaskUpdatedEvent(TaskId));
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
