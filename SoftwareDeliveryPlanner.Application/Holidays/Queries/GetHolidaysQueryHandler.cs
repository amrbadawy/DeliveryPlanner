using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Application.Holidays.Queries;

public sealed class GetHolidaysQueryHandler : IRequestHandler<GetHolidaysQuery, List<Holiday>>
{
    private readonly IHolidayOrchestrator _orchestrator;

    public GetHolidaysQueryHandler(IHolidayOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public Task<List<Holiday>> Handle(GetHolidaysQuery request, CancellationToken cancellationToken)
        => _orchestrator.GetHolidaysAsync(cancellationToken);
}
