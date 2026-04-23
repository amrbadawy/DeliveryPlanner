using FluentValidation.TestHelper;
using SoftwareDeliveryPlanner.Application.Tasks.Commands;

namespace SoftwareDeliveryPlanner.Tests.ValidatorTests;

public class BulkImportTasksCommandValidatorTests
{
    private readonly BulkImportTasksCommandValidator _validator = new();

    private static List<EffortBreakdownInput> ValidEB() => new()
    {
        new EffortBreakdownInput("DEV", 5, 0),
        new EffortBreakdownInput("QA", 1, 0)
    };

    private static BulkImportTasksCommand Valid() => new(new List<BulkTaskRowDto>
    {
        new("SVC-001", "Test Service", 1, ValidEB(), null)
    });

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var result = _validator.TestValidate(Valid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyList_FailsValidation()
    {
        var result = _validator.TestValidate(new BulkImportTasksCommand(new List<BulkTaskRowDto>()));
        result.ShouldHaveValidationErrorFor(c => c.Tasks);
    }

    [Fact]
    public void EmptyTaskId_FailsValidation()
    {
        var command = Valid() with { Tasks = new List<BulkTaskRowDto> { new("", "Test Service", 1, ValidEB(), null) } };
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void EmptyServiceName_FailsValidation()
    {
        var command = Valid() with { Tasks = new List<BulkTaskRowDto> { new("SVC-001", "", 1, ValidEB(), null) } };
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void EffortBreakdownEmpty_FailsValidation()
    {
        var command = Valid() with { Tasks = new List<BulkTaskRowDto> { new("SVC-001", "Test Service", 1, new List<EffortBreakdownInput>(), null) } };
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void EffortBreakdownZeroEstimation_FailsValidation()
    {
        var badEB = new List<EffortBreakdownInput>
        {
            new("DEV", 0, 0),
            new("QA", 0, 0)
        };
        var command = Valid() with { Tasks = new List<BulkTaskRowDto> { new("SVC-001", "Test Service", 1, badEB, null) } };
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
    }
}
