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
        DomainConstants.SettingKeys.AtRiskThreshold,
        DomainConstants.SettingKeys.WeekNumbering,
        DomainConstants.SettingKeys.GanttZoomLevel,
        DomainConstants.SettingKeys.ScenarioGanttZoomLevel
    };

    public UpsertSettingCommandValidator(ILookupOrchestrator? lookupOrchestrator = null)
    {
        RuleFor(c => c.Key)
            .NotEmpty().WithMessage("Setting key is required.")
            .Must(k => AllowedKeys.Contains(k)).WithMessage("Unknown setting key.");

        // Centralised value validation for enum-style settings.
        RuleFor(c => c)
            .Must(cmd =>
            {
                if (string.IsNullOrWhiteSpace(cmd.Value)) return true; // allow clear
                return cmd.Key switch
                {
                    var k when string.Equals(k, DomainConstants.SettingKeys.WeekNumbering, StringComparison.OrdinalIgnoreCase)
                        => DomainConstants.WeekNumbering.IsValid(cmd.Value),
                    var k when string.Equals(k, DomainConstants.SettingKeys.GanttZoomLevel, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(k, DomainConstants.SettingKeys.ScenarioGanttZoomLevel, StringComparison.OrdinalIgnoreCase)
                        => DomainConstants.GanttZoomLevel.IsValid(cmd.Value),
                    _ => true
                };
            })
            .WithName("Value")
            .WithMessage("Invalid value for the given setting key.");

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
