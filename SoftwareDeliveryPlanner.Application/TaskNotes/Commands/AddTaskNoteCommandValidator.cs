using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.TaskNotes.Commands;

public sealed class AddTaskNoteCommandValidator : AbstractValidator<AddTaskNoteCommand>
{
    public AddTaskNoteCommandValidator()
    {
        RuleFor(c => c.TaskId).NotEmpty().WithMessage("Task ID is required.");
        RuleFor(c => c.NoteText).NotEmpty().WithMessage("Note text is required.");
        RuleFor(c => c.Author).NotEmpty().WithMessage("Author is required.");
    }
}
