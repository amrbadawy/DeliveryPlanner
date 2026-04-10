using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.Holidays.Commands;

public sealed class UpsertHolidayCommandValidator : AbstractValidator<UpsertHolidayCommand>
{
    public UpsertHolidayCommandValidator()
    {
        RuleFor(c => c.HolidayName).NotEmpty().WithMessage("Holiday Name is required.");
    }
}
