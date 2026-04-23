using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Tasks.Commands;

public sealed record UpdateTaskEffortBreakdownCommand(
    string TaskId,
    List<EffortBreakdownInput> EffortBreakdown,
    bool RunScheduler) : IRequest<Result>;

internal sealed class UpdateTaskEffortBreakdownCommandHandler
    : IRequestHandler<UpdateTaskEffortBreakdownCommand, Result>
{
    private readonly ITaskOrchestrator _orchestrator;

    public UpdateTaskEffortBreakdownCommandHandler(ITaskOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result> Handle(UpdateTaskEffortBreakdownCommand request, CancellationToken cancellationToken)
    {
        var breakdown = request.EffortBreakdown
            .Select(e => (e.Role, e.EstimationDays, e.OverlapPct, e.MaxFte, e.MinSeniority))
            .ToList();

        await _orchestrator.UpdateTaskEffortBreakdownAsync(
            request.TaskId,
            breakdown,
            request.RunScheduler,
            cancellationToken);

        return Result.Success();
    }
}
