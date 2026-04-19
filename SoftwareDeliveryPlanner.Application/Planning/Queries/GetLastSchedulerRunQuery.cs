using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Planning.Queries;

public sealed record GetLastSchedulerRunQuery : IRequest<Result<DateTime?>>;

internal sealed class GetLastSchedulerRunQueryHandler : IRequestHandler<GetLastSchedulerRunQuery, Result<DateTime?>>
{
    private readonly IPlanningQueryService _service;

    public GetLastSchedulerRunQueryHandler(IPlanningQueryService service)
        => _service = service;

    public async Task<Result<DateTime?>> Handle(GetLastSchedulerRunQuery request, CancellationToken cancellationToken)
        => await _service.GetLastSchedulerRunAsync(cancellationToken);
}
