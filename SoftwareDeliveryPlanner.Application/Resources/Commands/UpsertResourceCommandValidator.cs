using FluentValidation;
using SoftwareDeliveryPlanner.Application.Abstractions;

namespace SoftwareDeliveryPlanner.Application.Resources.Commands;

public sealed class UpsertResourceCommandValidator : AbstractValidator<UpsertResourceCommand>
{
    public UpsertResourceCommandValidator(IRoleOrchestrator roleOrchestrator)
    {
        RuleFor(c => c.ResourceId).NotEmpty().WithMessage("Resource ID is required.");
        RuleFor(c => c.ResourceName).NotEmpty().WithMessage("Name is required.");
        RuleFor(c => c.Role).NotEmpty().WithMessage("Role is required.");
        RuleFor(c => c.AvailabilityPct)
            .InclusiveBetween(0, 100)
            .WithMessage("Availability must be between 0 and 100.");
        RuleFor(c => c.DailyCapacity).GreaterThan(0).WithMessage("Daily capacity must be greater than zero.");

        RuleFor(c => c)
            .MustAsync(async (cmd, ct) =>
            {
                if (string.IsNullOrWhiteSpace(cmd.Role))
                    return false;

                var roles = await roleOrchestrator.GetRolesAsync(false, ct);
                return roles.Any(r => r.Code == cmd.Role && r.IsActive);
            })
            .WithName("Role")
            .WithMessage("Selected role is invalid or inactive.");
    }
}
