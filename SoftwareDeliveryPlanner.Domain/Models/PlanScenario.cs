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
}
