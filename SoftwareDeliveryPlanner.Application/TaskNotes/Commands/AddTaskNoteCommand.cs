using MediatR;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.TaskNotes.Commands;

public sealed record AddTaskNoteCommand(string TaskId, string NoteText, string Author) : IRequest<Result>;
