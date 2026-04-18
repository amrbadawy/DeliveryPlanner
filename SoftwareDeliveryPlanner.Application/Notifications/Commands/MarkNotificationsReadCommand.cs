using MediatR;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Notifications.Commands;

public sealed record MarkNotificationsReadCommand : IRequest<Result>;
