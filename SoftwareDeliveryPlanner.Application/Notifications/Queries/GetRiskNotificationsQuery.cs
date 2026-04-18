using MediatR;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Notifications.Queries;

public sealed record GetRiskNotificationsQuery(bool UnreadOnly = true) : IRequest<Result<List<RiskNotification>>>;
