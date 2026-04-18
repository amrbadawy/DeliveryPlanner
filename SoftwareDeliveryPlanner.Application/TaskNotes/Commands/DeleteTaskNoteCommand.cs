using MediatR;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.TaskNotes.Commands;

public sealed record DeleteTaskNoteCommand(int Id) : IRequest<Result>;
