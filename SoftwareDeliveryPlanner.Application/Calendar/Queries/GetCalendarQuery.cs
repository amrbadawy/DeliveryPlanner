using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Application.Calendar.Queries;

public sealed record GetCalendarQuery : IRequest<List<CalendarDay>>;

internal sealed class GetCalendarQueryHandler : IRequestHandler<GetCalendarQuery, List<CalendarDay>>
{
    private readonly IPlanningQueryService _orchestrator;

    public GetCalendarQueryHandler(IPlanningQueryService orchestrator)
        => _orchestrator = orchestrator;

    public Task<List<CalendarDay>> Handle(GetCalendarQuery request, CancellationToken cancellationToken)
        => _orchestrator.GetCalendarAsync(cancellationToken);
}
