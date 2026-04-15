using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.Resources.Commands;

public sealed class DeleteResourceCommandValidator : AbstractValidator<DeleteResourceCommand>
{
    public DeleteResourceCommandValidator()
    {
        RuleFor(c => c.Id).GreaterThan(0).WithMessage("A valid resource ID is required for deletion.");
    }
}
