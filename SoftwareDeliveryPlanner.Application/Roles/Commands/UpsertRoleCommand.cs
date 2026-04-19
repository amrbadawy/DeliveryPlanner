using MediatR;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Roles.Commands;

public sealed record UpsertRoleCommand(
    int Id,
    string Code,
    string DisplayName,
    bool IsActive,
    int SortOrder,
    bool IsNew) : IRequest<Result>;

public sealed record DeleteRoleCommand(int Id) : IRequest<Result>;
