using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.Tasks.Commands;

public sealed class DeleteTaskCommandValidator : AbstractValidator<DeleteTaskCommand>
{
    public DeleteTaskCommandValidator()
    {
        RuleFor(c => c.Id).GreaterThan(0).WithMessage("A valid task ID is required for deletion.");
    }
}
