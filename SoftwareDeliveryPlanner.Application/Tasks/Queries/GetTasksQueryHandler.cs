using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Tasks.Queries;

internal sealed class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, Result<List<TaskItem>>>
{
    private readonly ITaskOrchestrator _orchestrator;

    public GetTasksQueryHandler(ITaskOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<List<TaskItem>>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
        => await _orchestrator.GetTasksAsync(cancellationToken);
}

internal sealed class GetTaskCountQueryHandler : IRequestHandler<GetTaskCountQuery, Result<int>>
{
    private readonly ITaskOrchestrator _orchestrator;

    public GetTaskCountQueryHandler(ITaskOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<int>> Handle(GetTaskCountQuery request, CancellationToken cancellationToken)
        => await _orchestrator.GetTaskCountAsync(cancellationToken);
}
