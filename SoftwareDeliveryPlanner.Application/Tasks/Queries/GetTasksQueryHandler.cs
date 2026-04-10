using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Application.Tasks.Queries;

public sealed class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, List<TaskItem>>
{
    private readonly ISchedulingOrchestrator _orchestrator;

    public GetTasksQueryHandler(ISchedulingOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public Task<List<TaskItem>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
        => _orchestrator.GetTasksAsync(cancellationToken);
}

public sealed class GetTaskCountQueryHandler : IRequestHandler<GetTaskCountQuery, int>
{
    private readonly ISchedulingOrchestrator _orchestrator;

    public GetTaskCountQueryHandler(ISchedulingOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public Task<int> Handle(GetTaskCountQuery request, CancellationToken cancellationToken)
        => _orchestrator.GetTaskCountAsync(cancellationToken);
}
