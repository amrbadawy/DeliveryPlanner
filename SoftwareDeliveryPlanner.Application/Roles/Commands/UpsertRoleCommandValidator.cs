using FluentValidation;
using SoftwareDeliveryPlanner.Application.Abstractions;

namespace SoftwareDeliveryPlanner.Application.Roles.Commands;

public sealed class UpsertRoleCommandValidator : AbstractValidator<UpsertRoleCommand>
{
    public UpsertRoleCommandValidator(IRoleOrchestrator orchestrator)
    {
        RuleFor(c => c.Code)
            .NotEmpty().WithMessage("Role code is required.")
            .MaximumLength(50).WithMessage("Role code must be 50 characters or fewer.");

        RuleFor(c => c.DisplayName)
            .NotEmpty().WithMessage("Role name is required.")
            .MaximumLength(100).WithMessage("Role name must be 100 characters or fewer.");

        RuleFor(c => c.SortOrder)
            .GreaterThan(0).WithMessage("Sort order must be greater than 0.");

        RuleFor(c => c)
            .MustAsync(async (cmd, ct) =>
            {
                var normalizedCode = cmd.Code.Trim();
                var exists = await orchestrator.RoleCodeExistsAsync(
                    normalizedCode,
                    cmd.IsNew ? null : cmd.Id,
                    ct);

                return !exists;
            })
            .WithName("Code")
            .WithMessage("A role with this code already exists.");

        RuleFor(c => c)
            .MustAsync(async (cmd, ct) =>
            {
                if (cmd.IsActive)
                    return true;

                var inUse = await orchestrator.IsRoleInUseAsync(cmd.Code.Trim(), ct);
                return !inUse;
            })
            .WithName("IsActive")
            .WithMessage("Cannot deactivate a role that is currently assigned to resources.");
    }
}
