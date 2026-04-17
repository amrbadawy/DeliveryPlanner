using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.Holidays.Commands;

public sealed class CopyHolidaysToYearCommandValidator : AbstractValidator<CopyHolidaysToYearCommand>
{
    public CopyHolidaysToYearCommandValidator()
    {
        RuleFor(c => c.SourceYear)
            .GreaterThan(0).WithMessage("Source year must be a positive number.");

        RuleFor(c => c.TargetYear)
            .GreaterThan(0).WithMessage("Target year must be a positive number.");

        RuleFor(c => c)
            .Must(c => c.SourceYear != c.TargetYear)
            .WithName("TargetYear")
            .WithMessage("Target year must be different from source year.");
    }
}
