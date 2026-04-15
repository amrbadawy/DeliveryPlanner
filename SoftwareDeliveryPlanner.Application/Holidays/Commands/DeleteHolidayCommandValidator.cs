using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.Holidays.Commands;

public sealed class DeleteHolidayCommandValidator : AbstractValidator<DeleteHolidayCommand>
{
    public DeleteHolidayCommandValidator()
    {
        RuleFor(c => c.Id).GreaterThan(0).WithMessage("A valid holiday ID is required for deletion.");
    }
}
