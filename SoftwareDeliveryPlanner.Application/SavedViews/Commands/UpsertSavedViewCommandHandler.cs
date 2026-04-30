using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.SavedViews.Commands;

internal sealed class UpsertSavedViewCommandHandler
    : IRequestHandler<UpsertSavedViewCommand, Result<SavedView>>
{
    private readonly ISavedViewOrchestrator _orchestrator;

    public UpsertSavedViewCommandHandler(ISavedViewOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<SavedView>> Handle(UpsertSavedViewCommand request, CancellationToken cancellationToken)
    {
        var view = await _orchestrator.UpsertAsync(request.Name, request.PageKey, request.PayloadJson, request.OwnerKey, cancellationToken);
        return Result<SavedView>.Success(view);
    }
}
