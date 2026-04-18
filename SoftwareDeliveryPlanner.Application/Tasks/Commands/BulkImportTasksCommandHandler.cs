using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Tasks.Commands;

internal sealed class BulkImportTasksCommandHandler : IRequestHandler<BulkImportTasksCommand, Result<int>>
{
    private readonly ITaskOrchestrator _orchestrator;

    public BulkImportTasksCommandHandler(ITaskOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<int>> Handle(BulkImportTasksCommand request, CancellationToken cancellationToken)
    {
        var count = 0;

        foreach (var row in request.Tasks)
        {
            var existing = await _orchestrator.GetTaskByTaskIdAsync(row.TaskId, cancellationToken);
            var isNew = existing is null;
            var id = existing?.Id ?? 0;

            await _orchestrator.UpsertTaskAsync(
                id, row.TaskId, row.ServiceName,
                row.DevEstimation, row.MaxDev, row.Priority,
                row.StrictDate, row.DependsOnTaskIds, isNew,
                cancellationToken);

            count++;
        }

        return Result<int>.Success(count);
    }
}
