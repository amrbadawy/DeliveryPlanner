using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Application.Tasks.Queries;

internal sealed class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, List<TaskItem>>
{
    private readonly ITaskOrchestrator _orchestrator;

    public GetTasksQueryHandler(ITaskOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public Task<List<TaskItem>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
        => _orchestrator.GetTasksAsync(cancellationToken);
}

internal sealed class GetTaskCountQueryHandler : IRequestHandler<GetTaskCountQuery, int>
{
    private readonly ITaskOrchestrator _orchestrator;

    public GetTaskCountQueryHandler(ITaskOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public Task<int> Handle(GetTaskCountQuery request, CancellationToken cancellationToken)
        => _orchestrator.GetTaskCountAsync(cancellationToken);
}
