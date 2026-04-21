using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Tasks.Commands;

internal sealed class UpsertTaskCommandHandler : IRequestHandler<UpsertTaskCommand, Result>
{
    private readonly ITaskOrchestrator _orchestrator;

    public UpsertTaskCommandHandler(ITaskOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result> Handle(UpsertTaskCommand request, CancellationToken cancellationToken)
    {
        var breakdown = request.EffortBreakdown
            .Select(e => (e.Role, e.EstimationDays, e.OverlapPct))
            .ToList();

        await _orchestrator.UpsertTaskAsync(
            request.Id, request.TaskId, request.ServiceName,
            request.MaxResource, request.Priority, breakdown,
            request.StrictDate, request.DependsOnTaskIds, request.IsNew,
            request.OverrideStart, request.Phase, request.PreferredResourceIds,
            cancellationToken);

        return Result.Success();
    }
}

internal sealed class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, Result>
{
    private readonly ITaskOrchestrator _orchestrator;

    public DeleteTaskCommandHandler(ITaskOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.DeleteTaskAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
