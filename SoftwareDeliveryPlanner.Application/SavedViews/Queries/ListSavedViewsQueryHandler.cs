using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.SavedViews.Queries;

internal sealed class ListSavedViewsQueryHandler
    : IRequestHandler<ListSavedViewsQuery, Result<List<SavedView>>>
{
    private readonly ISavedViewOrchestrator _orchestrator;

    public ListSavedViewsQueryHandler(ISavedViewOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<List<SavedView>>> Handle(ListSavedViewsQuery request, CancellationToken cancellationToken)
    {
        var views = await _orchestrator.ListAsync(request.PageKey, request.OwnerKey, cancellationToken);
        return Result<List<SavedView>>.Success(views);
    }
}
