using MediatR;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.TaskNotes.Queries;

public sealed record GetTaskNotesQuery(string TaskId) : IRequest<Result<List<TaskNote>>>;
