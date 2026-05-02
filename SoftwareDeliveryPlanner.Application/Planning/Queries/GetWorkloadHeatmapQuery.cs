using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Planning.Queries;

public sealed record WorkloadCellDto(string ResourceId, string ResourceName, DateTime WeekStart, double UtilizationPct);

public sealed record WorkloadHeatmapDto(
    List<string> ResourceNames,
    List<DateTime> WeekStarts,
    List<WorkloadCellDto> Cells);

public sealed record GetWorkloadHeatmapQuery : IRequest<Result<WorkloadHeatmapDto>>;

internal sealed class GetWorkloadHeatmapQueryHandler : IRequestHandler<GetWorkloadHeatmapQuery, Result<WorkloadHeatmapDto>>
{
    /// <summary>Operation key used by <see cref="ITestFaultPolicy"/> for this handler.</summary>
    public const string FaultOperationKey = "WorkloadHeatmap";

    private readonly IPlanningQueryService _service;
    private readonly ITestFaultPolicy _testFaultPolicy;

    public GetWorkloadHeatmapQueryHandler(IPlanningQueryService service, ITestFaultPolicy testFaultPolicy)
    {
        _service = service;
        _testFaultPolicy = testFaultPolicy;
    }

    public async Task<Result<WorkloadHeatmapDto>> Handle(GetWorkloadHeatmapQuery request, CancellationToken cancellationToken)
    {
        // Test-only fault seam: in production this is always a no-op (NoOpTestFaultPolicy).
        // In e2e runs with SDP_TEST_FAULTS=1, an armed key throws TestInjectedFaultException,
        // which the Heatmap page catches and surfaces as the error-state UI.
        _testFaultPolicy.MaybeThrow(FaultOperationKey);

        return await _service.GetWorkloadHeatmapAsync(cancellationToken);
    }
}
