using FluentValidation.TestHelper;
using SoftwareDeliveryPlanner.Application.Scenarios.Commands;

namespace SoftwareDeliveryPlanner.Tests.ValidatorTests;

public class DeleteScenarioCommandValidatorTests
{
    private readonly DeleteScenarioCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var result = _validator.TestValidate(new DeleteScenarioCommand(1));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ZeroId_FailsValidation()
    {
        var result = _validator.TestValidate(new DeleteScenarioCommand(0));
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void NegativeId_FailsValidation()
    {
        var result = _validator.TestValidate(new DeleteScenarioCommand(-1));
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }
}
