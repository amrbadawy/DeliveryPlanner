using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.SavedViews.Commands;

internal sealed class RenameSavedViewCommandHandler
    : IRequestHandler<RenameSavedViewCommand, Result<SavedView>>
{
    private readonly ISavedViewOrchestrator _orchestrator;

    public RenameSavedViewCommandHandler(ISavedViewOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<SavedView>> Handle(RenameSavedViewCommand request, CancellationToken cancellationToken)
    {
        var existing = await _orchestrator.GetByIdAsync(request.Id, cancellationToken);
        if (existing is null)
            return Result<SavedView>.Failure(Error.NotFound("Saved view was not found."));

        var view = await _orchestrator.RenameAsync(request.Id, request.Name, cancellationToken);
        if (view is null)
            return Result<SavedView>.Failure(Error.Conflict("A saved view with this name already exists for this page."));

        return Result<SavedView>.Success(view);
    }
}
