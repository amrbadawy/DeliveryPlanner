using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Application.Calendar.Queries;

public sealed record GetCalendarQuery : IRequest<List<CalendarDay>>;

public sealed class GetCalendarQueryHandler : IRequestHandler<GetCalendarQuery, List<CalendarDay>>
{
    private readonly ISchedulingOrchestrator _orchestrator;

    public GetCalendarQueryHandler(ISchedulingOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public Task<List<CalendarDay>> Handle(GetCalendarQuery request, CancellationToken cancellationToken)
        => _orchestrator.GetCalendarAsync(cancellationToken);
}
