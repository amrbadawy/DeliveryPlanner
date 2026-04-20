using MediatR;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Resources.Queries;

public sealed record GetResourcesQuery : IRequest<Result<List<TeamMember>>>;

public sealed record GetResourceCountQuery : IRequest<Result<int>>;

public sealed record GetResourceByIdQuery(string ResourceId) : IRequest<Result<TeamMember?>>;
