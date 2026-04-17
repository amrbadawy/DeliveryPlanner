using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Timeline.Queries;

public sealed record GetTimelineQuery(
    string ResourceId,
    DateTime Start,
    DateTime End) : IRequest<Result<TimelineDataDto>>;

internal sealed class GetTimelineQueryHandler : IRequestHandler<GetTimelineQuery, Result<TimelineDataDto>>
{
    private readonly IPlanningQueryService _orchestrator;

    public GetTimelineQueryHandler(IPlanningQueryService orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<TimelineDataDto>> Handle(GetTimelineQuery request, CancellationToken cancellationToken)
        => await _orchestrator.GetTimelineDataAsync(request.ResourceId, request.Start, request.End, cancellationToken);
}
