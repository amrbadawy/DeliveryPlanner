using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.TaskNotes.Commands;

internal sealed class AddTaskNoteCommandHandler : IRequestHandler<AddTaskNoteCommand, Result>
{
    private readonly ITaskNoteOrchestrator _orchestrator;

    public AddTaskNoteCommandHandler(ITaskNoteOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result> Handle(AddTaskNoteCommand request, CancellationToken cancellationToken)
    {
        var note = TaskNote.Create(request.TaskId, request.NoteText, request.Author);
        await _orchestrator.AddNoteAsync(note);
        return Result.Success();
    }
}
