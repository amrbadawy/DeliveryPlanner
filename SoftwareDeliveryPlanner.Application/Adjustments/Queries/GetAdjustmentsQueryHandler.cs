using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Adjustments.Queries;

internal sealed class GetAdjustmentsQueryHandler : IRequestHandler<GetAdjustmentsQuery, Result<List<Adjustment>>>
{
    private readonly IAdjustmentOrchestrator _orchestrator;

    public GetAdjustmentsQueryHandler(IAdjustmentOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<List<Adjustment>>> Handle(GetAdjustmentsQuery request, CancellationToken cancellationToken)
        => await _orchestrator.GetAdjustmentsAsync(cancellationToken);
}
