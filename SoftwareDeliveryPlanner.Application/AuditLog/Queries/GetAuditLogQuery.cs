using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.AuditLog.Queries;

public sealed record GetAuditLogQuery(int Count = 50) : IRequest<Result<List<AuditEntry>>>;

internal sealed class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, Result<List<AuditEntry>>>
{
    private readonly IAuditService _auditService;

    public GetAuditLogQueryHandler(IAuditService auditService) => _auditService = auditService;

    public async Task<Result<List<AuditEntry>>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
        => await _auditService.GetRecentAsync(request.Count);
}
