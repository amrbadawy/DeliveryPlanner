using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Application.Tasks.Commands;

public sealed class UpsertTaskCommandHandler : IRequestHandler<UpsertTaskCommand, Unit>
{
    private readonly ISchedulingOrchestrator _orchestrator;

    public UpsertTaskCommandHandler(ISchedulingOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Unit> Handle(UpsertTaskCommand request, CancellationToken cancellationToken)
    {
        var task = new TaskItem
        {
            Id = request.Id,
            TaskId = request.TaskId,
            ServiceName = request.ServiceName,
            DevEstimation = request.DevEstimation,
            MaxDev = request.MaxDev,
            Priority = request.Priority,
            StrictDate = request.StrictDate
        };

        await _orchestrator.UpsertTaskAsync(task, request.IsNew, cancellationToken);
        return Unit.Value;
    }
}

public sealed class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, Unit>
{
    private readonly ISchedulingOrchestrator _orchestrator;

    public DeleteTaskCommandHandler(ISchedulingOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Unit> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.DeleteTaskAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
