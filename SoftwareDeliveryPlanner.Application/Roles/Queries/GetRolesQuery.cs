using MediatR;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Roles.Queries;

public sealed record GetRolesQuery(bool IncludeInactive = true) : IRequest<Result<List<Role>>>;
