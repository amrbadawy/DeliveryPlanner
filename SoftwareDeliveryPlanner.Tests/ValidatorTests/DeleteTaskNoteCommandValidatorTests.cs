using FluentValidation.TestHelper;
using SoftwareDeliveryPlanner.Application.TaskNotes.Commands;

namespace SoftwareDeliveryPlanner.Tests.ValidatorTests;

public class DeleteTaskNoteCommandValidatorTests
{
    private readonly DeleteTaskNoteCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var result = _validator.TestValidate(new DeleteTaskNoteCommand(1));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ZeroId_FailsValidation()
    {
        var result = _validator.TestValidate(new DeleteTaskNoteCommand(0));
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void NegativeId_FailsValidation()
    {
        var result = _validator.TestValidate(new DeleteTaskNoteCommand(-1));
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }
}
