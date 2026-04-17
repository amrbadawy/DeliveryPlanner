using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Calendar.Queries;

public sealed record GetCalendarQuery : IRequest<Result<List<CalendarDay>>>;

internal sealed class GetCalendarQueryHandler : IRequestHandler<GetCalendarQuery, Result<List<CalendarDay>>>
{
    private readonly IPlanningQueryService _orchestrator;

    public GetCalendarQueryHandler(IPlanningQueryService orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<List<CalendarDay>>> Handle(GetCalendarQuery request, CancellationToken cancellationToken)
        => await _orchestrator.GetCalendarAsync(cancellationToken);
}
