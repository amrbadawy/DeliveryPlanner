using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.Resources.Commands;

public sealed class UpsertResourceCommandValidator : AbstractValidator<UpsertResourceCommand>
{
    public UpsertResourceCommandValidator()
    {
        RuleFor(c => c.ResourceId).NotEmpty().WithMessage("Resource ID is required.");
        RuleFor(c => c.ResourceName).NotEmpty().WithMessage("Name is required.");
        RuleFor(c => c.AvailabilityPct)
            .InclusiveBetween(0, 100)
            .WithMessage("Availability must be between 0 and 100.");
        RuleFor(c => c.DailyCapacity).GreaterThan(0).WithMessage("Daily capacity must be greater than zero.");
    }
}
