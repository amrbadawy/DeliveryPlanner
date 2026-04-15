using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;

namespace SoftwareDeliveryPlanner.Application.Timeline.Queries;

public sealed record GetTimelineQuery(
    string ResourceId,
    DateTime Start,
    DateTime End) : IRequest<TimelineDataDto>;

internal sealed class GetTimelineQueryHandler : IRequestHandler<GetTimelineQuery, TimelineDataDto>
{
    private readonly IPlanningQueryService _orchestrator;

    public GetTimelineQueryHandler(IPlanningQueryService orchestrator)
        => _orchestrator = orchestrator;

    public Task<TimelineDataDto> Handle(GetTimelineQuery request, CancellationToken cancellationToken)
        => _orchestrator.GetTimelineDataAsync(request.ResourceId, request.Start, request.End, cancellationToken);
}
