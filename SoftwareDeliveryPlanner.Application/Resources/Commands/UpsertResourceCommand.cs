using MediatR;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Resources.Commands;

public sealed record UpsertResourceCommand(
    int Id,
    string ResourceId,
    string ResourceName,
    string Role,
    string Team,
    double AvailabilityPct,
    double DailyCapacity,
    DateTime StartDate,
    string Active,
    string? Notes,
    bool IsNew) : IRequest<Result>;

public sealed record DeleteResourceCommand(int Id) : IRequest<Result>;
