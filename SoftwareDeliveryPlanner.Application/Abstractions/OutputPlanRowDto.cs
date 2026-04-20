namespace SoftwareDeliveryPlanner.Application.Abstractions;

/// <summary>
/// Typed DTO for a single row in the delivery output plan.
/// Replaces the untyped <c>Dictionary&lt;string, object?&gt;</c> that was previously
/// returned by <see cref="IPlanningQueryService.GetOutputPlanAsync"/>.
/// </summary>
public sealed record OutputPlanRowDto(
    int Num,
    string TaskId,
    string ServiceName,
    double? AssignedResource,
    string? PlannedStart,
    string? PlannedFinish,
    int? Duration,
    double DevEstimation,
    string? StrictDate,
    string Status,
    string DeliveryRisk);
