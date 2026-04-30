using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.SavedViews.Commands;

internal sealed class DeleteSavedViewCommandHandler : IRequestHandler<DeleteSavedViewCommand, Result>
{
    private readonly ISavedViewOrchestrator _orchestrator;

    public DeleteSavedViewCommandHandler(ISavedViewOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result> Handle(DeleteSavedViewCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.DeleteAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
