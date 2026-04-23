namespace SoftwareDeliveryPlanner.Domain.Models;

/// <summary>
/// Specifies a single role's effort contribution when creating or updating a task.
/// Replaces the anonymous tuple previously used at call sites.
/// </summary>
public sealed record EffortBreakdownSpec(
    string Role,
    double EstimationDays,
    double OverlapPct,
    double MaxFte = 1.0,
    string? MinSeniority = null);
