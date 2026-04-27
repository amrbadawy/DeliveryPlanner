using FluentValidation;
using SoftwareDeliveryPlanner.Domain;

namespace SoftwareDeliveryPlanner.Application.Tasks.Commands;

public sealed class BulkImportTasksCommandValidator : AbstractValidator<BulkImportTasksCommand>
{
    public BulkImportTasksCommandValidator()
    {
        RuleFor(c => c.Tasks).NotEmpty().WithMessage("At least one task is required.");

        RuleFor(c => c.Tasks)
            .Must(tasks => tasks is null || tasks.Select(t => t.TaskId).Distinct(StringComparer.OrdinalIgnoreCase).Count() == tasks.Count)
            .WithMessage("Duplicate Task IDs found in import batch.");

        RuleForEach(c => c.Tasks).ChildRules(row =>
        {
            row.RuleFor(r => r.TaskId).NotEmpty().WithMessage("Task ID is required.").MaximumLength(20).WithMessage("Task ID must not exceed 20 characters.");
            row.RuleFor(r => r.ServiceName).NotEmpty().WithMessage("Service Name is required.").MaximumLength(200).WithMessage("Service Name must not exceed 200 characters.");
            row.RuleFor(r => r.Priority).InclusiveBetween(1, 10).WithMessage("Priority must be between 1 and 10.");

            row.RuleFor(r => r.EffortBreakdown)
                .NotNull().WithMessage("Effort breakdown is required.")
                .Must(e => e is not null && e.Count >= 2).WithMessage("Effort breakdown must contain at least 2 entries.");

            row.RuleFor(r => r.EffortBreakdown)
                .Must(e => e is not null && e.Any(r => r.Role.Equals("DEV", StringComparison.OrdinalIgnoreCase) && r.EstimationDays > 0))
                .WithMessage("Effort breakdown must include a DEV entry with EstimationDays > 0.");

            row.RuleFor(r => r.EffortBreakdown)
                .Must(e => e is not null && e.Any(r => r.Role.Equals("QA", StringComparison.OrdinalIgnoreCase) && r.EstimationDays > 0))
                .WithMessage("Effort breakdown must include a QA entry with EstimationDays > 0.");

            row.RuleFor(r => r.EffortBreakdown)
                .Must(e => e is null || e.Select(r => r.Role.ToUpperInvariant()).Distinct().Count() == e.Count)
                .WithMessage("Duplicate roles are not allowed in effort breakdown.");

            row.RuleForEach(r => r.EffortBreakdown).ChildRules(entry =>
            {
                entry.RuleFor(e => e.Role).NotEmpty().WithMessage("Role is required.");
                entry.RuleFor(e => e.Role)
                    .Must(r => string.IsNullOrWhiteSpace(r) || DomainConstants.ResourceRole.AllRoles.Contains(r))
                    .WithMessage(e => $"Invalid role '{e.Role}'.");
                entry.RuleFor(e => e.EstimationDays).GreaterThan(0).WithMessage("EstimationDays must be greater than zero.");
                entry.RuleFor(e => e.OverlapPct).InclusiveBetween(0, 100).WithMessage("OverlapPct must be between 0 and 100.");
                entry.RuleFor(e => e.MaxFte).GreaterThan(0).WithMessage("Max FTE must be greater than zero.");
            });
        });
    }
}
