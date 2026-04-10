using MediatR;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Application.Tasks.Queries;

public sealed record GetTasksQuery : IRequest<List<TaskItem>>;

public sealed record GetTaskCountQuery : IRequest<int>;
