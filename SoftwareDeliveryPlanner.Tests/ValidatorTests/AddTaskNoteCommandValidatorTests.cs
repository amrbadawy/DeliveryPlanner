using FluentValidation.TestHelper;
using SoftwareDeliveryPlanner.Application.TaskNotes.Commands;

namespace SoftwareDeliveryPlanner.Tests.ValidatorTests;

public class AddTaskNoteCommandValidatorTests
{
    private readonly AddTaskNoteCommandValidator _validator = new();

    private static AddTaskNoteCommand Valid() => new("SVC-001", "Test note", "Developer");

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var result = _validator.TestValidate(Valid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTaskId_FailsValidation()
    {
        var result = _validator.TestValidate(Valid() with { TaskId = "" });
        result.ShouldHaveValidationErrorFor(c => c.TaskId);
    }

    [Fact]
    public void EmptyNoteText_FailsValidation()
    {
        var result = _validator.TestValidate(Valid() with { NoteText = "" });
        result.ShouldHaveValidationErrorFor(c => c.NoteText);
    }

    [Fact]
    public void EmptyAuthor_FailsValidation()
    {
        var result = _validator.TestValidate(Valid() with { Author = "" });
        result.ShouldHaveValidationErrorFor(c => c.Author);
    }
}
