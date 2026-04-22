using FluentValidation;

namespace SoftwareDeliveryPlanner.Application.Tasks.Commands;

public sealed class UpsertTaskCommandValidator : AbstractValidator<UpsertTaskCommand>
{
    public UpsertTaskCommandValidator()
    {
        RuleFor(c => c.TaskId).NotEmpty().WithMessage("Service ID is required.");
        RuleFor(c => c.ServiceName).NotEmpty().WithMessage("Service Name is required.");
        RuleFor(c => c.MaxResource).GreaterThan(0).WithMessage("Max Resources must be greater than zero.");
        RuleFor(c => c.Priority).InclusiveBetween(1, 10).WithMessage("Priority must be between 1 and 10.");

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
        });

        When(c => c.Dependencies is not null, () =>
        {
            RuleForEach(c => c.Dependencies!).ChildRules(dep =>
            {
                dep.RuleFor(d => d.PredecessorTaskId).NotEmpty().WithMessage("PredecessorTaskId is required.");
                dep.RuleFor(d => d.Type).NotEmpty().WithMessage("Dependency type is required.");
                dep.RuleFor(d => d.LagDays).GreaterThanOrEqualTo(0).WithMessage("LagDays must be >= 0.");
                dep.RuleFor(d => d.OverlapPct).InclusiveBetween(0, 100).WithMessage("Dependency OverlapPct must be between 0 and 100.");
            });
        });
    }
}
