using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.SavedViews.Commands;

public sealed class RenameSavedViewCommandValidator : AbstractValidator<RenameSavedViewCommand>
{
    public RenameSavedViewCommandValidator()
    {
        RuleFor(c => c.Id)
            .GreaterThan(0).WithMessage("Saved view id must be a positive integer.");

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Saved view name is required.")
            .MaximumLength(100).WithMessage("Saved view name must be 100 characters or fewer.");
    }
}
