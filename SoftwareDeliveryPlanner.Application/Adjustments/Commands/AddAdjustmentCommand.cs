using MediatR;

namespace SoftwareDeliveryPlanner.Application.Adjustments.Commands;

public sealed record AddAdjustmentCommand(
    string ResourceId,
    string AdjType,
    double AvailabilityPct,
    DateTime AdjStart,
    DateTime AdjEnd,
    string? Notes) : IRequest<Unit>;

public sealed record DeleteAdjustmentCommand(int Id) : IRequest<Unit>;
