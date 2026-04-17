using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Holidays.Queries;

internal sealed class GetHolidaysQueryHandler : IRequestHandler<GetHolidaysQuery, Result<List<Holiday>>>
{
    private readonly IHolidayOrchestrator _orchestrator;

    public GetHolidaysQueryHandler(IHolidayOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<List<Holiday>>> Handle(GetHolidaysQuery request, CancellationToken cancellationToken)
        => await _orchestrator.GetHolidaysAsync(cancellationToken);
}
