using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Planning.Commands;

internal sealed class RunSchedulerCommandHandler : IRequestHandler<RunSchedulerCommand, Result<string>>
{
    private readonly ISchedulerService _orchestrator;

    public RunSchedulerCommandHandler(ISchedulerService orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<string>> Handle(RunSchedulerCommand request, CancellationToken cancellationToken)
    {
        var result = await _orchestrator.RunSchedulerAsync(cancellationToken);
        return result;
    }
}
