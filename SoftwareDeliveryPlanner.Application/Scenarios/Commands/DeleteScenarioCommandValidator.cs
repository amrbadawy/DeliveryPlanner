using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.Scenarios.Commands;

public sealed class DeleteScenarioCommandValidator : AbstractValidator<DeleteScenarioCommand>
{
    public DeleteScenarioCommandValidator()
    {
        RuleFor(c => c.Id).GreaterThan(0).WithMessage("A valid scenario ID is required for deletion.");
    }
}
