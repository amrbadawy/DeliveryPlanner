using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.Tasks.Commands;

public sealed class BulkImportTasksCommandValidator : AbstractValidator<BulkImportTasksCommand>
{
    public BulkImportTasksCommandValidator()
    {
        RuleFor(c => c.Tasks).NotEmpty().WithMessage("At least one task is required.");

        RuleForEach(c => c.Tasks).ChildRules(row =>
        {
            row.RuleFor(r => r.TaskId).NotEmpty().WithMessage("Task ID is required.");
            row.RuleFor(r => r.ServiceName).NotEmpty().WithMessage("Service Name is required.");
            row.RuleFor(r => r.DevEstimation).GreaterThan(0).WithMessage("Estimation must be greater than zero.");
            row.RuleFor(r => r.MaxResource).GreaterThanOrEqualTo(0).WithMessage("Max Resources must not be negative.");
            row.RuleFor(r => r.Priority).GreaterThan(0).WithMessage("Priority must be greater than zero.");
        });
    }
}
