using FluentValidation.TestHelper;
using SoftwareDeliveryPlanner.Application.Tasks.Commands;

namespace SoftwareDeliveryPlanner.Tests.ValidatorTests;

public class BulkImportTasksCommandValidatorTests
{
    private readonly BulkImportTasksCommandValidator _validator = new();

    private static BulkImportTasksCommand Valid() => new(new List<BulkTaskRowDto>
    {
        new("SVC-001", "Test Service", 5.0, 1.0, 1, null, null)
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
        var command = Valid() with { Tasks = new List<BulkTaskRowDto> { new("", "Test Service", 5.0, 1.0, 1, null, null) } };
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void EmptyServiceName_FailsValidation()
    {
        var command = Valid() with { Tasks = new List<BulkTaskRowDto> { new("SVC-001", "", 5.0, 1.0, 1, null, null) } };
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void DevEstimationZero_FailsValidation()
    {
        var command = Valid() with { Tasks = new List<BulkTaskRowDto> { new("SVC-001", "Test Service", 0, 1.0, 1, null, null) } };
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void DevEstimationNegative_FailsValidation()
    {
        var command = Valid() with { Tasks = new List<BulkTaskRowDto> { new("SVC-001", "Test Service", -1, 1.0, 1, null, null) } };
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
    }
}
