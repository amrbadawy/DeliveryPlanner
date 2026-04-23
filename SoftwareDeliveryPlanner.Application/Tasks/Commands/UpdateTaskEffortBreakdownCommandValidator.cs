using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.Tasks.Commands;

public sealed class UpdateTaskEffortBreakdownCommandValidator : AbstractValidator<UpdateTaskEffortBreakdownCommand>
{
    public UpdateTaskEffortBreakdownCommandValidator()
    {
        RuleFor(c => c.TaskId)
            .NotEmpty().WithMessage("Task ID is required.");

        RuleFor(c => c.EffortBreakdown)
            .NotNull().WithMessage("Effort breakdown is required.")
            .Must(e => e is not null && e.Count >= 2).WithMessage("Effort breakdown must contain at least 2 entries.");

        RuleFor(c => c.EffortBreakdown)
            .Must(e => e is not null && e.Any(r => r.Role.Equals("DEV", StringComparison.OrdinalIgnoreCase) && r.EstimationDays > 0))
            .WithMessage("Effort breakdown must include a DEV entry with EstimationDays > 0.");

        RuleFor(c => c.EffortBreakdown)
            .Must(e => e is not null && e.Any(r => r.Role.Equals("QA", StringComparison.OrdinalIgnoreCase) && r.EstimationDays > 0))
            .WithMessage("Effort breakdown must include a QA entry with EstimationDays > 0.");

        RuleFor(c => c.EffortBreakdown)
            .Must(e => e is null || e.Select(r => r.Role.ToUpperInvariant()).Distinct().Count() == e.Count)
            .WithMessage("Duplicate roles are not allowed in effort breakdown.");

        RuleForEach(c => c.EffortBreakdown).ChildRules(entry =>
        {
            entry.RuleFor(e => e.Role).NotEmpty().WithMessage("Role is required.");
            entry.RuleFor(e => e.EstimationDays).GreaterThan(0).WithMessage("EstimationDays must be greater than zero.");
            entry.RuleFor(e => e.OverlapPct).InclusiveBetween(0, 100).WithMessage("OverlapPct must be between 0 and 100.");
            entry.RuleFor(e => e.MaxFte).GreaterThan(0).WithMessage("Max FTE must be greater than zero.");
        });
    }
}
