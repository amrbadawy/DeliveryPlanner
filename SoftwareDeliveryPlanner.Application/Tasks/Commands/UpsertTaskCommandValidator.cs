using FluentValidation;
using SoftwareDeliveryPlanner.Application.Tasks.Commands;

namespace SoftwareDeliveryPlanner.Application.Tasks.Commands;

public sealed class UpsertTaskCommandValidator : AbstractValidator<UpsertTaskCommand>
{
    public UpsertTaskCommandValidator()
    {
        RuleFor(c => c.TaskId).NotEmpty().WithMessage("Service ID is required.");
        RuleFor(c => c.ServiceName).NotEmpty().WithMessage("Service Name is required.");
        RuleFor(c => c.DevEstimation).GreaterThan(0).WithMessage("Estimation must be greater than zero.");
        RuleFor(c => c.MaxResource).GreaterThan(0).WithMessage("Max Resources must be greater than zero.");
        RuleFor(c => c.Priority).InclusiveBetween(1, 10).WithMessage("Priority must be between 1 and 10.");
    }
}
