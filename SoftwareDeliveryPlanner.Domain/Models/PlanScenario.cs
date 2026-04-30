using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Models;

public class PlanScenario
{
    public int Id { get; private set; }
    public string ScenarioName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public int TotalTasks { get; private set; }
    public int OnTrackCount { get; private set; }
    public int AtRiskCount { get; private set; }
    public int LateCount { get; private set; }
    public int UnscheduledCount { get; private set; }
    public DateTime? EarliestStart { get; private set; }
    public DateTime? LatestFinish { get; private set; }
    public double TotalEstimation { get; private set; }
    public string? Notes { get; private set; }
    public string? GanttZoomLevel { get; private set; }

    // ── Task snapshots (historical Gantt data) ──────────────
    private readonly List<ScenarioTaskSnapshot> _taskSnapshots = new();
    public IReadOnlyCollection<ScenarioTaskSnapshot> TaskSnapshots => _taskSnapshots.AsReadOnly();

    private PlanScenario() { }

    public static PlanScenario Create(string scenarioName, int totalTasks, int onTrack, int atRisk, int late, int unscheduled,
        DateTime? earliestStart, DateTime? latestFinish, double totalEstimation, string? notes, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(scenarioName))
            throw new DomainException("Scenario name is required.");

        if (totalTasks < 0)
            throw new DomainException("Total tasks must not be negative.");
        if (onTrack < 0)
            throw new DomainException("On track count must not be negative.");
        if (atRisk < 0)
            throw new DomainException("At risk count must not be negative.");
        if (late < 0)
            throw new DomainException("Late count must not be negative.");
        if (unscheduled < 0)
            throw new DomainException("Unscheduled count must not be negative.");
        if (onTrack + atRisk + late + unscheduled != totalTasks)
            throw new DomainException("Status counts (on track + at risk + late + unscheduled) must equal total tasks.");
        if (totalEstimation < 0)
            throw new DomainException("Total estimation must not be negative.");
        if (earliestStart.HasValue && latestFinish.HasValue && earliestStart.Value > latestFinish.Value)
            throw new DomainException("Earliest start must not be after latest finish.");

        return new PlanScenario
        {
            ScenarioName = scenarioName.Trim(),
            CreatedAt = createdAt,
            TotalTasks = totalTasks,
            OnTrackCount = onTrack,
            AtRiskCount = atRisk,
            LateCount = late,
            UnscheduledCount = unscheduled,
            EarliestStart = earliestStart,
            LatestFinish = latestFinish,
            TotalEstimation = totalEstimation,
            Notes = notes
        };
    }

    /// <summary>
    /// Adds a task snapshot to this scenario.
    /// The snapshot must reference this scenario's Id.
    /// </summary>
    public void AddTaskSnapshot(ScenarioTaskSnapshot snapshot)
    {
        if (snapshot is null)
            throw new DomainException("Task snapshot must not be null.");

        _taskSnapshots.Add(snapshot);
    }

    public void SetGanttZoomLevel(string? zoomLevel)
    {
        if (string.IsNullOrWhiteSpace(zoomLevel))
        {
            GanttZoomLevel = null;
            return;
        }

        var canonical = zoomLevel.Trim().ToUpperInvariant();
        if (!DomainConstants.GanttZoomLevel.IsValid(canonical))
            throw new DomainException("Invalid scenario zoom level.");

        GanttZoomLevel = canonical;
    }
}
