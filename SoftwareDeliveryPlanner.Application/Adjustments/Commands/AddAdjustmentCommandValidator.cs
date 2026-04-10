using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.Adjustments.Commands;

public sealed class AddAdjustmentCommandValidator : AbstractValidator<AddAdjustmentCommand>
{
    public AddAdjustmentCommandValidator()
    {
        RuleFor(c => c.ResourceId).NotEmpty().WithMessage("Employee is required.");
        RuleFor(c => c.AvailabilityPct)
            .InclusiveBetween(0, 100)
            .WithMessage("Availability must be between 0 and 100.");
        RuleFor(c => c.AdjEnd)
            .GreaterThanOrEqualTo(c => c.AdjStart)
            .WithMessage("Start Date must be before or equal to End Date.");
    }
}
