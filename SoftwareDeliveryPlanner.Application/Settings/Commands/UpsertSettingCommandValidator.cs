using FluentValidation;
using SoftwareDeliveryPlanner.Application.Abstractions;
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

    public UpsertSettingCommandValidator(ILookupOrchestrator? lookupOrchestrator = null)
    {
        RuleFor(c => c.Key)
            .NotEmpty().WithMessage("Setting key is required.")
            .Must(k => AllowedKeys.Contains(k)).WithMessage("Unknown setting key.");

        if (lookupOrchestrator is not null)
        {
            RuleFor(c => c)
                .MustAsync(async (cmd, ct) =>
                {
                    if (!string.Equals(cmd.Key, DomainConstants.SettingKeys.WorkingWeek, StringComparison.OrdinalIgnoreCase))
                        return true;

                    if (string.IsNullOrWhiteSpace(cmd.Value))
                        return false;

                    return await lookupOrchestrator.IsActiveLookupValueAsync(LookupCatalogs.WorkingWeeks, cmd.Value, ct);
                })
                .WithName("Value")
                .WithMessage("Working week must be an active lookup value.");
        }
    }
}
