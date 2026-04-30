using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.SavedViews.Commands;

public sealed class SetDefaultSavedViewCommandValidator : AbstractValidator<SetDefaultSavedViewCommand>
{
    public SetDefaultSavedViewCommandValidator()
    {
        RuleFor(c => c.Id)
            .GreaterThan(0).WithMessage("Saved view id must be a positive integer.");
    }
}
