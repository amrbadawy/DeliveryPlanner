using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Output.Queries;

public sealed record GetOutputPlanQuery : IRequest<Result<List<OutputPlanRowDto>>>;

internal sealed class GetOutputPlanQueryHandler : IRequestHandler<GetOutputPlanQuery, Result<List<OutputPlanRowDto>>>
{
    private readonly IPlanningQueryService _orchestrator;

    public GetOutputPlanQueryHandler(IPlanningQueryService orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<List<OutputPlanRowDto>>> Handle(GetOutputPlanQuery request, CancellationToken cancellationToken)
        => await _orchestrator.GetOutputPlanAsync(cancellationToken);
}
