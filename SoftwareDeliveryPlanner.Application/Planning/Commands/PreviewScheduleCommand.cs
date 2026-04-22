using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Planning.Commands;

public sealed record PreviewScheduleCommand() : IRequest<Result<ScheduleDiffDto>>;

internal sealed class PreviewScheduleCommandHandler : IRequestHandler<PreviewScheduleCommand, Result<ScheduleDiffDto>>
{
    private readonly ISchedulerService _scheduler;
    public PreviewScheduleCommandHandler(ISchedulerService scheduler) => _scheduler = scheduler;
    public async Task<Result<ScheduleDiffDto>> Handle(PreviewScheduleCommand request, CancellationToken cancellationToken)
    {
        var diff = await _scheduler.PreviewScheduleAsync(cancellationToken);
        return Result<ScheduleDiffDto>.Success(diff);
    }
}
