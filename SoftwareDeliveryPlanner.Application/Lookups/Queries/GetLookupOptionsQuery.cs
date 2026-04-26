using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Lookups.Queries;

public sealed record GetLookupOptionsQuery(string Catalog, bool IncludeInactive = false) : IRequest<Result<List<LookupOptionDto>>>;

internal sealed class GetLookupOptionsQueryHandler : IRequestHandler<GetLookupOptionsQuery, Result<List<LookupOptionDto>>>
{
    private readonly ILookupOrchestrator _orchestrator;

    public GetLookupOptionsQueryHandler(ILookupOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<List<LookupOptionDto>>> Handle(GetLookupOptionsQuery request, CancellationToken cancellationToken)
        => await _orchestrator.GetLookupOptionsAsync(request.Catalog, request.IncludeInactive, cancellationToken);
}
