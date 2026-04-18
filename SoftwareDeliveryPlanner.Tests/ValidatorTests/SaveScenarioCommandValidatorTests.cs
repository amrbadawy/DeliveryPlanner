using FluentValidation.TestHelper;
using SoftwareDeliveryPlanner.Application.Scenarios.Commands;

namespace SoftwareDeliveryPlanner.Tests.ValidatorTests;

public class SaveScenarioCommandValidatorTests
{
    private readonly SaveScenarioCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var result = _validator.TestValidate(new SaveScenarioCommand("Baseline", null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyName_FailsValidation()
    {
        var result = _validator.TestValidate(new SaveScenarioCommand("", null));
        result.ShouldHaveValidationErrorFor(c => c.ScenarioName);
    }

    [Fact]
    public void WhitespaceName_FailsValidation()
    {
        var result = _validator.TestValidate(new SaveScenarioCommand("   ", null));
        result.ShouldHaveValidationErrorFor(c => c.ScenarioName);
    }
}
