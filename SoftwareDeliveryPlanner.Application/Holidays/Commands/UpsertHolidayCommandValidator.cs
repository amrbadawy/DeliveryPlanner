using FluentValidation;
using SoftwareDeliveryPlanner.Application.Abstractions;

namespace SoftwareDeliveryPlanner.Application.Holidays.Commands;

public sealed class UpsertHolidayCommandValidator : AbstractValidator<UpsertHolidayCommand>
{
    public UpsertHolidayCommandValidator(IHolidayOrchestrator orchestrator, ILookupOrchestrator? lookupOrchestrator = null)
    {
        RuleFor(c => c.HolidayName)
            .NotEmpty().WithMessage("Holiday Name is required.");

        RuleFor(c => c.HolidayType)
            .NotEmpty().WithMessage("Holiday type is required.");

        if (lookupOrchestrator is not null)
        {
            RuleFor(c => c.HolidayType)
                .MustAsync(async (value, ct) => await lookupOrchestrator.IsActiveLookupValueAsync(LookupCatalogs.HolidayTypes, value, ct))
                .WithMessage("Selected holiday type is invalid or inactive.");
        }

        RuleFor(c => c.StartDate)
            .LessThanOrEqualTo(c => c.EndDate)
            .WithMessage("Start date must be on or before end date.");

        RuleFor(c => c)
            .MustAsync(async (cmd, ct) =>
            {
                var hasOverlap = await orchestrator.HasHolidayOverlapAsync(
                    cmd.StartDate, cmd.EndDate,
                    cmd.IsNew ? null : cmd.Id,
                    ct);
                return !hasOverlap;
            })
            .WithName("StartDate")
            .WithMessage("This holiday overlaps with an existing holiday.");
    }
}
