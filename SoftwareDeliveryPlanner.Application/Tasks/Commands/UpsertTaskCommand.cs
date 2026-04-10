using MediatR;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Application.Tasks.Commands;

public sealed record UpsertTaskCommand(
    int Id,
    string TaskId,
    string ServiceName,
    double DevEstimation,
    double MaxDev,
    int Priority,
    DateTime? StrictDate,
    bool IsNew) : IRequest<Unit>;

public sealed record DeleteTaskCommand(int Id) : IRequest<Unit>;
