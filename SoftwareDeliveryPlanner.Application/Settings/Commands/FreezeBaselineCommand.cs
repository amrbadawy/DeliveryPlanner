using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Settings.Commands;

public sealed record FreezeBaselineCommand : IRequest<Result>;

internal sealed class FreezeBaselineCommandHandler : IRequestHandler<FreezeBaselineCommand, Result>
{
    private readonly ISchedulerService _schedulerService;

    public FreezeBaselineCommandHandler(ISchedulerService schedulerService) => _schedulerService = schedulerService;

    public async Task<Result> Handle(FreezeBaselineCommand request, CancellationToken cancellationToken)
    {
        await _schedulerService.FreezeBaselineAsync(cancellationToken);
        return Result.Success();
    }
}
