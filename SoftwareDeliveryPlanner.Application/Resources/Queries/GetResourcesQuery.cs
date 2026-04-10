using MediatR;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Application.Resources.Queries;

public sealed record GetResourcesQuery : IRequest<List<TeamMember>>;

public sealed record GetResourceCountQuery : IRequest<int>;
