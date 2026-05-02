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
    /// <summary>Operation key used by <see cref="ITestFaultPolicy"/> for this handler.</summary>
    public const string FaultOperationKey = "GanttSegments";

    private readonly IPlanningQueryService _planningQueryService;
    private readonly ITestFaultPolicy _testFaultPolicy;

    public GetGanttSegmentsQueryHandler(
        IPlanningQueryService planningQueryService,
        ITestFaultPolicy testFaultPolicy)
    {
        _planningQueryService = planningQueryService;
        _testFaultPolicy = testFaultPolicy;
    }

    public async Task<Result<List<TaskGanttSegmentsDto>>> Handle(GetGanttSegmentsQuery request, CancellationToken cancellationToken)
    {
        // Test-only fault seam: in production this is always a no-op (NoOpTestFaultPolicy).
        // In e2e runs with SDP_TEST_FAULTS=1, an armed key throws TestInjectedFaultException,
        // which the Gantt page catches and surfaces as the error-state UI.
        _testFaultPolicy.MaybeThrow(FaultOperationKey);

        var segments = await _planningQueryService.GetGanttSegmentsAsync(cancellationToken);
        return Result<List<TaskGanttSegmentsDto>>.Success(segments);
    }
}
