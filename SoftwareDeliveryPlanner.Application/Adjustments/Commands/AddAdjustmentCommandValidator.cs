using FluentValidation;
using SoftwareDeliveryPlanner.Application.Abstractions;

namespace SoftwareDeliveryPlanner.Application.Adjustments.Commands;

public sealed class AddAdjustmentCommandValidator : AbstractValidator<AddAdjustmentCommand>
{
    public AddAdjustmentCommandValidator(ILookupOrchestrator? lookupOrchestrator = null)
    {
        RuleFor(c => c.ResourceId).NotEmpty().WithMessage("Employee is required.");
        RuleFor(c => c.AdjType)
            .NotEmpty().WithMessage("Adjustment type is required.");

        if (lookupOrchestrator is not null)
        {
            RuleFor(c => c.AdjType)
                .MustAsync(async (value, ct) => await lookupOrchestrator.IsActiveLookupValueAsync(LookupCatalogs.AdjustmentTypes, value, ct))
                .WithMessage("Selected adjustment type is invalid or inactive.");
        }
        RuleFor(c => c.AvailabilityPct)
            .InclusiveBetween(0, 100)
            .WithMessage("Availability must be between 0 and 100.");
        RuleFor(c => c.AdjEnd)
            .GreaterThanOrEqualTo(c => c.AdjStart)
            .WithMessage("Start Date must be before or equal to End Date.");
    }
}
