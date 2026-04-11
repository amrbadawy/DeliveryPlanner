using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;

namespace SoftwareDeliveryPlanner.Application.Output.Queries;

public sealed record GetOutputPlanQuery : IRequest<List<Dictionary<string, object?>>>;

public sealed class GetOutputPlanQueryHandler : IRequestHandler<GetOutputPlanQuery, List<Dictionary<string, object?>>>
{
    private readonly IPlanningQueryService _orchestrator;

    public GetOutputPlanQueryHandler(IPlanningQueryService orchestrator)
        => _orchestrator = orchestrator;

    public Task<List<Dictionary<string, object?>>> Handle(GetOutputPlanQuery request, CancellationToken cancellationToken)
        => _orchestrator.GetOutputPlanAsync(cancellationToken);
}
