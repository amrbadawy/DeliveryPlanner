using FluentValidation;
using SoftwareDeliveryPlanner.Domain;

namespace SoftwareDeliveryPlanner.Application.Scenarios.Commands;

public sealed class SetScenarioZoomLevelCommandValidator : AbstractValidator<SetScenarioZoomLevelCommand>
{
    public SetScenarioZoomLevelCommandValidator()
    {
        RuleFor(c => c.ScenarioId)
            .GreaterThan(0).WithMessage("Scenario id must be a positive integer.");

        RuleFor(c => c.ZoomLevel)
            .Must(v => string.IsNullOrWhiteSpace(v) || DomainConstants.GanttZoomLevel.IsValid(v))
            .WithMessage("Scenario zoom level is invalid.");
    }
}
