using MediatR;

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
    bool IsNew) : IRequest<Unit>;

public sealed record DeleteResourceCommand(int Id) : IRequest<Unit>;
