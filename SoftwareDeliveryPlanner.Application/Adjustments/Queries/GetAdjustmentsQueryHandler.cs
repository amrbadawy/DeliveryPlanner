using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Application.Adjustments.Queries;

public sealed class GetAdjustmentsQueryHandler : IRequestHandler<GetAdjustmentsQuery, List<Adjustment>>
{
    private readonly IAdjustmentOrchestrator _orchestrator;

    public GetAdjustmentsQueryHandler(IAdjustmentOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public Task<List<Adjustment>> Handle(GetAdjustmentsQuery request, CancellationToken cancellationToken)
        => _orchestrator.GetAdjustmentsAsync(cancellationToken);
}
