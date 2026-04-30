using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.SavedViews.Commands;

internal sealed class SetDefaultSavedViewCommandHandler
    : IRequestHandler<SetDefaultSavedViewCommand, Result<SavedView>>
{
    private readonly ISavedViewOrchestrator _orchestrator;

    public SetDefaultSavedViewCommandHandler(ISavedViewOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<SavedView>> Handle(SetDefaultSavedViewCommand request, CancellationToken cancellationToken)
    {
        var existing = await _orchestrator.GetByIdAsync(request.Id, cancellationToken);
        if (existing is null)
            return Result<SavedView>.Failure(Error.NotFound("Saved view was not found."));

        var updated = await _orchestrator.SetDefaultAsync(request.Id, request.IsDefault, cancellationToken);
        if (updated is null)
            return Result<SavedView>.Failure(Error.NotFound("Saved view was not found."));

        return Result<SavedView>.Success(updated);
    }
}
