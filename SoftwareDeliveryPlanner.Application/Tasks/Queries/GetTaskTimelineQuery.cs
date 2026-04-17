using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Tasks.Queries;

public sealed record GetTaskTimelineQuery(string TaskId) : IRequest<Result<TaskTimelineDto>>;

internal sealed class GetTaskTimelineQueryHandler : IRequestHandler<GetTaskTimelineQuery, Result<TaskTimelineDto>>
{
    private readonly IPlanningQueryService _orchestrator;

    public GetTaskTimelineQueryHandler(IPlanningQueryService orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<TaskTimelineDto>> Handle(GetTaskTimelineQuery request, CancellationToken cancellationToken)
        => await _orchestrator.GetTaskTimelineAsync(request.TaskId, cancellationToken);
}