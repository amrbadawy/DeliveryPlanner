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
    private readonly IPlanningQueryService _service;

    public GetWorkloadHeatmapQueryHandler(IPlanningQueryService service)
        => _service = service;

    public async Task<Result<WorkloadHeatmapDto>> Handle(GetWorkloadHeatmapQuery request, CancellationToken cancellationToken)
        => await _service.GetWorkloadHeatmapAsync(cancellationToken);
}
