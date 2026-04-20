using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Models;

/// <summary>
/// Point-in-time snapshot of a single task's schedule data,
/// captured when a <see cref="PlanScenario"/> is saved.
/// Enables historical Gantt chart views per scenario.
/// </summary>
public class ScenarioTaskSnapshot
{
    public int Id { get; private set; }
    public int PlanScenarioId { get; private set; }

    // ── Task identity ────────────────────────────────────────
    public string TaskId { get; private set; } = string.Empty;
    public string ServiceName { get; private set; } = string.Empty;
    public int Priority { get; private set; }
    public int? SchedulingRank { get; private set; }

    // ── Schedule data ────────────────────────────────────────
    public DateTime? PlannedStart { get; private set; }
    public DateTime? PlannedFinish { get; private set; }
    public int? Duration { get; private set; }
    public DateTime? StrictDate { get; private set; }

    // ── Allocation ───────────────────────────────────────────
    public string? AssignedResourceId { get; private set; }
    public double? AssignedResource { get; private set; }
    public double DevEstimation { get; private set; }
    public double MaxResource { get; private set; }

    // ── Status ───────────────────────────────────────────────
    public string Status { get; private set; } = string.Empty;
    public string DeliveryRisk { get; private set; } = string.Empty;

    // ── Dependencies ─────────────────────────────────────────
    public string? DependsOnTaskIds { get; private set; }

    // ── Navigation ───────────────────────────────────────────
    public PlanScenario Scenario { get; private set; } = null!;

    private ScenarioTaskSnapshot() { }

    public static ScenarioTaskSnapshot Create(
        int planScenarioId,
        string taskId,
        string serviceName,
        int priority,
        int? schedulingRank,
        DateTime? plannedStart,
        DateTime? plannedFinish,
        int? duration,
        DateTime? strictDate,
        string? assignedResourceId,
        double? assignedResource,
        double devEstimation,
        double maxResource,
        string status,
        string deliveryRisk,
        string? dependsOnTaskIds)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            throw new DomainException("Task ID is required for a snapshot.");

        return new ScenarioTaskSnapshot
        {
            PlanScenarioId = planScenarioId,
            TaskId = taskId.Trim(),
            ServiceName = (serviceName ?? string.Empty).Trim(),
            Priority = priority,
            SchedulingRank = schedulingRank,
            PlannedStart = plannedStart,
            PlannedFinish = plannedFinish,
            Duration = duration,
            StrictDate = strictDate,
            AssignedResourceId = assignedResourceId,
            AssignedResource = assignedResource,
            DevEstimation = devEstimation,
            MaxResource = maxResource,
            Status = status ?? string.Empty,
            DeliveryRisk = deliveryRisk ?? string.Empty,
            DependsOnTaskIds = dependsOnTaskIds
        };
    }
}
