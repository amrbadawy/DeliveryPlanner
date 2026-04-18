using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.TaskNotes.Queries;

internal sealed class GetTaskNotesQueryHandler : IRequestHandler<GetTaskNotesQuery, Result<List<TaskNote>>>
{
    private readonly ITaskNoteOrchestrator _orchestrator;

    public GetTaskNotesQueryHandler(ITaskNoteOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<List<TaskNote>>> Handle(GetTaskNotesQuery request, CancellationToken cancellationToken)
        => await _orchestrator.GetNotesAsync(request.TaskId);
}
