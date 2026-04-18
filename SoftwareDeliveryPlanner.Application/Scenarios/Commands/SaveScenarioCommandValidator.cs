using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.Scenarios.Commands;

public sealed class SaveScenarioCommandValidator : AbstractValidator<SaveScenarioCommand>
{
    public SaveScenarioCommandValidator()
    {
        RuleFor(c => c.ScenarioName).NotEmpty().WithMessage("Scenario name is required.");
    }
}
