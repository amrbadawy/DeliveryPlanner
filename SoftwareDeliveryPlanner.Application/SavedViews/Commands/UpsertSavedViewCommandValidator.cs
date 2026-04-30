using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.SavedViews.Commands;

public sealed class UpsertSavedViewCommandValidator : AbstractValidator<UpsertSavedViewCommand>
{
    private static readonly HashSet<string> AllowedPageKeys = new(StringComparer.OrdinalIgnoreCase)
        { "tasks", "gantt" };

    public UpsertSavedViewCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Saved view name is required.")
            .MaximumLength(100).WithMessage("Saved view name must be 100 characters or fewer.");

        RuleFor(c => c.PageKey)
            .NotEmpty().WithMessage("Page key is required.")
            .Must(key => AllowedPageKeys.Contains(key))
            .WithMessage($"Page key must be one of: {string.Join(", ", AllowedPageKeys)}.");

        RuleFor(c => c.PayloadJson)
            .NotEmpty().WithMessage("Payload is required.");

        RuleFor(c => c.OwnerKey)
            .MaximumLength(256).WithMessage("Owner key must be 256 characters or fewer.")
            .When(c => c.OwnerKey is not null);
    }
}
