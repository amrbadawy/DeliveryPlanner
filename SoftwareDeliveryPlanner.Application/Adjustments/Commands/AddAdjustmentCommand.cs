using MediatR;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Adjustments.Commands;

public sealed record AddAdjustmentCommand(
    string ResourceId,
    string AdjType,
    double AvailabilityPct,
    DateTime AdjStart,
    DateTime AdjEnd,
    string? Notes) : IRequest<Result>;

public sealed record DeleteAdjustmentCommand(int Id) : IRequest<Result>;
