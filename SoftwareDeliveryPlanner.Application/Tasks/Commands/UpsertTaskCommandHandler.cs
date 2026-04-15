using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Application.Tasks.Commands;

internal sealed class UpsertTaskCommandHandler : IRequestHandler<UpsertTaskCommand, Unit>
{
    private readonly ITaskOrchestrator _orchestrator;

    public UpsertTaskCommandHandler(ITaskOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Unit> Handle(UpsertTaskCommand request, CancellationToken cancellationToken)
    {
        var task = TaskItem.Create(
            request.TaskId,
            request.ServiceName,
            request.DevEstimation,
            request.MaxDev,
            request.Priority,
            request.StrictDate,
            request.DependsOnTaskIds);

        task.Id = request.Id;

        await _orchestrator.UpsertTaskAsync(task, request.IsNew, cancellationToken);
        return Unit.Value;
    }
}

internal sealed class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, Unit>
{
    private readonly ITaskOrchestrator _orchestrator;

    public DeleteTaskCommandHandler(ITaskOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Unit> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.DeleteTaskAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
