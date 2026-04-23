using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
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

            var breakdown = row.EffortBreakdown
                .Select(e => (e.Role, e.EstimationDays, e.OverlapPct, e.MaxFte))
                .ToList();

            var dependencies = row.Dependencies?
                .Select(d => (d.PredecessorTaskId, d.Type, d.LagDays, d.OverlapPct))
                .ToList();

            await _orchestrator.UpsertTaskAsync(
                id, row.TaskId, row.ServiceName,
                row.Priority, breakdown,
                row.StrictDate, dependencies, isNew,
                phase: row.Phase, preferredResourceIds: row.PreferredResourceIds,
                cancellationToken: cancellationToken);

            count++;
        }

        return Result<int>.Success(count);
    }
}
