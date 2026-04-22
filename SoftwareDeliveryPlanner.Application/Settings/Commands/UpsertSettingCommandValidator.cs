using FluentValidation;
using SoftwareDeliveryPlanner.Domain;

namespace SoftwareDeliveryPlanner.Application.Settings.Commands;

public sealed class UpsertSettingCommandValidator : AbstractValidator<UpsertSettingCommand>
{
    private static readonly HashSet<string> AllowedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        DomainConstants.SettingKeys.SchedulingStrategy,
        DomainConstants.SettingKeys.BaselineDate,
        DomainConstants.SettingKeys.WorkingWeek,
        DomainConstants.SettingKeys.PlanStartDate,
        DomainConstants.SettingKeys.AtRiskThreshold
    };

    public UpsertSettingCommandValidator()
    {
        RuleFor(c => c.Key)
            .NotEmpty().WithMessage("Setting key is required.")
            .Must(k => AllowedKeys.Contains(k)).WithMessage("Unknown setting key.");
    }
}
