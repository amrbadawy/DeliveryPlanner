using FluentValidation;
using SoftwareDeliveryPlanner.Application.Abstractions;

namespace SoftwareDeliveryPlanner.Application.Roles.Commands;

public sealed class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator(IRoleOrchestrator orchestrator)
    {
        RuleFor(c => c.Id).GreaterThan(0).WithMessage("A valid role ID is required for deletion.");

        RuleFor(c => c)
            .MustAsync(async (cmd, ct) =>
            {
                var roles = await orchestrator.GetRolesAsync(true, ct);
                var role = roles.FirstOrDefault(r => r.Id == cmd.Id);
                if (role is null)
                    return true;

                var inUse = await orchestrator.IsRoleInUseAsync(role.Code, ct);
                return !inUse;
            })
            .WithName("Id")
            .WithMessage("Cannot delete a role that is currently assigned to resources.");
    }
}
