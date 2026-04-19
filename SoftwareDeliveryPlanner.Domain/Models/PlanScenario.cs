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
    public DateTime? EarliestStart { get; private set; }
    public DateTime? LatestFinish { get; private set; }
    public double TotalEstimation { get; private set; }
    public string? Notes { get; private set; }

    // ── Task snapshots (historical Gantt data) ──────────────
    private readonly List<ScenarioTaskSnapshot> _taskSnapshots = new();
    public IReadOnlyCollection<ScenarioTaskSnapshot> TaskSnapshots => _taskSnapshots.AsReadOnly();

    private PlanScenario() { }

    public static PlanScenario Create(string scenarioName, int totalTasks, int onTrack, int atRisk, int late,
        DateTime? earliestStart, DateTime? latestFinish, double totalEstimation, string? notes, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(scenarioName))
            throw new DomainException("Scenario name is required.");

        return new PlanScenario
        {
            ScenarioName = scenarioName.Trim(),
            CreatedAt = createdAt,
            TotalTasks = totalTasks,
            OnTrackCount = onTrack,
            AtRiskCount = atRisk,
            LateCount = late,
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
}
