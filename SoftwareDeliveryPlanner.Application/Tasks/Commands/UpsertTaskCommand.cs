using MediatR;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Tasks.Commands;

public sealed record UpsertTaskCommand(
    int Id,
    string TaskId,
    string ServiceName,
    double DevEstimation,
    double MaxDev,
    int Priority,
    DateTime? StrictDate,
    string? DependsOnTaskIds,
    bool IsNew) : IRequest<Result>;

public sealed record DeleteTaskCommand(int Id) : IRequest<Result>;
