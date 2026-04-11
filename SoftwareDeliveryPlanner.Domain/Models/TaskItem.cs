using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.SharedKernel;
using SoftwareDeliveryPlanner.Domain.SharedKernel.ValueObjects;
using TaskIdVO = SoftwareDeliveryPlanner.Domain.SharedKernel.ValueObjects.TaskId;

namespace SoftwareDeliveryPlanner.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string TaskId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public double DevEstimation { get; set; }
    public double MaxDev { get; set; } = 1.0;
    public DateTime? StrictDate { get; set; }
    public int Priority { get; set; } = 5;
    public int? SchedulingRank { get; set; }
    public double? AssignedDev { get; set; }
    public string? AssignedResourceId { get; set; }
    public DateTime? PlannedStart { get; set; }
    public DateTime? PlannedFinish { get; set; }
    public int? Duration { get; set; }
    public string Status { get; set; } = DomainConstants.TaskStatus.NotStarted;
    public string DeliveryRisk { get; set; } = DomainConstants.DeliveryRisk.OnTrack;
    public DateTime? OverrideStart { get; set; }
    public double? OverrideDev { get; set; }
    public string? DependsOnTaskIds { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // ── Domain factory ────────────────────────────────────────────────────────
    /// <summary>
    /// Creates and validates a new <see cref="TaskItem"/> using domain invariants.
    /// Raises <see cref="DomainException"/> on any violation.
    /// </summary>
    public static TaskItem Create(
        string taskId,
        string serviceName,
        double devEstimation,
        double maxDev,
        int priority,
        DateTime? strictDate = null,
        string? dependsOnTaskIds = null)
    {
        if (!TaskIdVO.TryCreate(taskId, out _))
            throw new DomainException($"Invalid Task ID '{taskId}'. Expected format: AAA-000.");

        if (string.IsNullOrWhiteSpace(serviceName))
            throw new DomainException("Service name must not be empty.");

        if (devEstimation <= 0)
            throw new DomainException("Dev estimation must be greater than zero.");

        if (maxDev <= 0)
            throw new DomainException("Max developers must be greater than zero.");

        if (priority < 1 || priority > 10)
            throw new DomainException("Priority must be between 1 and 10.");

        return new TaskItem
        {
            TaskId = taskId.Trim().ToUpperInvariant(),
            ServiceName = serviceName.Trim(),
            DevEstimation = devEstimation,
            MaxDev = maxDev,
            Priority = priority,
            StrictDate = strictDate,
            DependsOnTaskIds = string.IsNullOrWhiteSpace(dependsOnTaskIds) ? null : dependsOnTaskIds.Trim()
        };
    }
}
