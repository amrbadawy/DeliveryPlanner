using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.Adjustments.Commands;

public sealed class DeleteAdjustmentCommandValidator : AbstractValidator<DeleteAdjustmentCommand>
{
    public DeleteAdjustmentCommandValidator()
    {
        RuleFor(c => c.Id).GreaterThan(0).WithMessage("A valid adjustment ID is required for deletion.");
    }
}
