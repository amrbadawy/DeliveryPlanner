namespace SoftwareDeliveryPlanner.Domain.Models;

/// <summary>
/// Specifies a predecessor dependency when creating or updating a task.
/// Replaces the anonymous tuple previously used at call sites.
/// </summary>
public sealed record DependencySpec(
    string PredecessorTaskId,
    string Type,
    int LagDays,
    double OverlapPct);
