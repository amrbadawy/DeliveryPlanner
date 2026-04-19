using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Roles.Commands;

internal sealed class UpsertRoleCommandHandler : IRequestHandler<UpsertRoleCommand, Result>
{
    private readonly IRoleOrchestrator _orchestrator;

    public UpsertRoleCommandHandler(IRoleOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result> Handle(UpsertRoleCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.UpsertRoleAsync(
            request.Id,
            request.Code,
            request.DisplayName,
            request.IsActive,
            request.SortOrder,
            request.IsNew,
            cancellationToken);

        return Result.Success();
    }
}

internal sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result>
{
    private readonly IRoleOrchestrator _orchestrator;

    public DeleteRoleCommandHandler(IRoleOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.DeleteRoleAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
