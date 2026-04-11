using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;

namespace SoftwareDeliveryPlanner.Application.Planning.Commands;

public sealed class RunSchedulerCommandHandler : IRequestHandler<RunSchedulerCommand, string>
{
    private readonly ISchedulerService _orchestrator;

    public RunSchedulerCommandHandler(ISchedulerService orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public async Task<string> Handle(RunSchedulerCommand request, CancellationToken cancellationToken)
    {
        return await _orchestrator.RunSchedulerAsync(cancellationToken);
    }
}
