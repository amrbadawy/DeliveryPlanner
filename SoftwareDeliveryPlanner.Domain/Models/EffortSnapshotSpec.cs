namespace SoftwareDeliveryPlanner.Domain.Models;

/// <summary>
/// Carries effort breakdown data when creating a <see cref="ScenarioTaskSnapshot"/>.
/// Replaces the anonymous tuple previously used at call sites.
/// </summary>
public sealed record EffortSnapshotSpec(
    string Role,
    double EstimationDays,
    double OverlapPct,
    int SortOrder,
    double MaxFte = 1.0);
