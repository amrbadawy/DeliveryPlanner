using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.TaskNotes.Commands;

public sealed class DeleteTaskNoteCommandValidator : AbstractValidator<DeleteTaskNoteCommand>
{
    public DeleteTaskNoteCommandValidator()
    {
        RuleFor(c => c.Id).GreaterThan(0).WithMessage("A valid note ID is required for deletion.");
    }
}
