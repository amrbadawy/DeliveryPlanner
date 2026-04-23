using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Tasks.Queries;

/// <summary>
/// Returns role-based Gantt segments for all scheduled tasks.
/// Each task gets a list of segments (one per role phase) with start/end dates,
/// MaxFte, and assigned resources — used to render sub-segments within each Gantt row.
/// </summary>
public sealed record GetGanttSegmentsQuery : IRequest<Result<List<TaskGanttSegmentsDto>>>;

internal sealed class GetGanttSegmentsQueryHandler : IRequestHandler<GetGanttSegmentsQuery, Result<List<TaskGanttSegmentsDto>>>
{
    private readonly IPlanningQueryService _planningQueryService;

    public GetGanttSegmentsQueryHandler(IPlanningQueryService planningQueryService)
        => _planningQueryService = planningQueryService;

    public async Task<Result<List<TaskGanttSegmentsDto>>> Handle(GetGanttSegmentsQuery request, CancellationToken cancellationToken)
        => await _planningQueryService.GetGanttSegmentsAsync(cancellationToken);
}
