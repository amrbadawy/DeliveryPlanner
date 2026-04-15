using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;

namespace SoftwareDeliveryPlanner.Application.Output.Queries;

public sealed record GetOutputPlanQuery : IRequest<List<OutputPlanRowDto>>;

internal sealed class GetOutputPlanQueryHandler : IRequestHandler<GetOutputPlanQuery, List<OutputPlanRowDto>>
{
    private readonly IPlanningQueryService _orchestrator;

    public GetOutputPlanQueryHandler(IPlanningQueryService orchestrator)
        => _orchestrator = orchestrator;

    public Task<List<OutputPlanRowDto>> Handle(GetOutputPlanQuery request, CancellationToken cancellationToken)
        => _orchestrator.GetOutputPlanAsync(cancellationToken);
}
