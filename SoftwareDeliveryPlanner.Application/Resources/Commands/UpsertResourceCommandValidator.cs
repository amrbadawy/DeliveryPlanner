using FluentValidation;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain;

namespace SoftwareDeliveryPlanner.Application.Resources.Commands;

public sealed class UpsertResourceCommandValidator : AbstractValidator<UpsertResourceCommand>
{
    public UpsertResourceCommandValidator(IRoleOrchestrator roleOrchestrator, ILookupOrchestrator? lookupOrchestrator = null)
    {
        RuleFor(c => c.ResourceId).NotEmpty().WithMessage("Resource ID is required.");
        RuleFor(c => c.ResourceName).NotEmpty().WithMessage("Name is required.");
        RuleFor(c => c.Role).NotEmpty().WithMessage("Role is required.");
        RuleFor(c => c.AvailabilityPct)
            .InclusiveBetween(0, 100)
            .WithMessage("Availability must be between 0 and 100.");
        RuleFor(c => c.DailyCapacity).GreaterThan(0).WithMessage("Daily capacity must be greater than zero.");

        RuleFor(c => c.SeniorityLevel)
            .Must(s => s is null || DomainConstants.Seniority.IsValid(s))
            .WithMessage(c => $"Invalid seniority level '{c.SeniorityLevel}'. Valid levels: {string.Join(", ", DomainConstants.Seniority.Levels)}.");

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

        RuleFor(c => c.Active)
            .NotEmpty().WithMessage("Active status is required.");

        if (lookupOrchestrator is not null)
        {
            RuleFor(c => c.Active)
                .MustAsync(async (value, ct) => await lookupOrchestrator.IsActiveLookupValueAsync(LookupCatalogs.ActiveStatuses, value, ct))
                .WithMessage("Selected active status is invalid or inactive.");

            RuleFor(c => c.WorkingWeek)
                .MustAsync(async (value, ct) =>
                {
                    if (string.IsNullOrWhiteSpace(value))
                        return true;

                    return await lookupOrchestrator.IsActiveLookupValueAsync(LookupCatalogs.WorkingWeeks, value, ct);
                })
                .WithMessage("Selected working week is invalid or inactive.");
        }
    }
}
