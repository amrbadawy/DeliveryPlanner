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
            row.RuleFor(r => r.MaxResource).GreaterThanOrEqualTo(0).WithMessage("Max Resources must not be negative.");
            row.RuleFor(r => r.Priority).GreaterThan(0).WithMessage("Priority must be greater than zero.");

            row.RuleFor(r => r.EffortBreakdown)
                .NotNull().WithMessage("Effort breakdown is required.")
                .Must(e => e is not null && e.Count >= 2).WithMessage("Effort breakdown must contain at least 2 entries.");

            row.RuleFor(r => r.EffortBreakdown)
                .Must(e => e is not null && e.Any(r => r.Role.Equals("DEV", StringComparison.OrdinalIgnoreCase) && r.EstimationDays > 0))
                .WithMessage("Effort breakdown must include a DEV entry with EstimationDays > 0.");

            row.RuleFor(r => r.EffortBreakdown)
                .Must(e => e is not null && e.Any(r => r.Role.Equals("QA", StringComparison.OrdinalIgnoreCase) && r.EstimationDays > 0))
                .WithMessage("Effort breakdown must include a QA entry with EstimationDays > 0.");

            row.RuleForEach(r => r.EffortBreakdown).ChildRules(entry =>
            {
                entry.RuleFor(e => e.Role).NotEmpty().WithMessage("Role is required.");
                entry.RuleFor(e => e.EstimationDays).GreaterThan(0).WithMessage("EstimationDays must be greater than zero.");
                entry.RuleFor(e => e.OverlapPct).InclusiveBetween(0, 100).WithMessage("OverlapPct must be between 0 and 100.");
            });
        });
    }
}
