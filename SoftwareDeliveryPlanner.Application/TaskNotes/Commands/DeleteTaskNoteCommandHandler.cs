using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.TaskNotes.Commands;

internal sealed class DeleteTaskNoteCommandHandler : IRequestHandler<DeleteTaskNoteCommand, Result>
{
    private readonly ITaskNoteOrchestrator _orchestrator;

    public DeleteTaskNoteCommandHandler(ITaskNoteOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result> Handle(DeleteTaskNoteCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.DeleteNoteAsync(request.Id);
        return Result.Success();
    }
}
