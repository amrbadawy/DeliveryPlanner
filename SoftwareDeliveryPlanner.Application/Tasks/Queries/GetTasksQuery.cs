using MediatR;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Tasks.Queries;

public sealed record GetTasksQuery : IRequest<Result<List<TaskItem>>>;

public sealed record GetTaskCountQuery : IRequest<Result<int>>;

public sealed record GetTaskByIdQuery(string TaskId) : IRequest<Result<TaskItem?>>;
