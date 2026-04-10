using FluentValidation;
using SoftwareDeliveryPlanner.Application.Abstractions;

namespace SoftwareDeliveryPlanner.Application.Holidays.Commands;

public sealed class UpsertHolidayCommandValidator : AbstractValidator<UpsertHolidayCommand>
{
    public UpsertHolidayCommandValidator(ISchedulingOrchestrator orchestrator)
    {
        RuleFor(c => c.HolidayName)
            .NotEmpty().WithMessage("Holiday Name is required.");

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
