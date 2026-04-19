using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Roles.Queries;

internal sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, Result<List<Role>>>
{
    private readonly IRoleOrchestrator _orchestrator;

    public GetRolesQueryHandler(IRoleOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<List<Role>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        => await _orchestrator.GetRolesAsync(request.IncludeInactive, cancellationToken);
}
