using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;

namespace SoftwareDeliveryPlanner.Application.DeliveryInsights.Queries;

public sealed record GetDashboardKpisQuery : IRequest<DashboardKpisDto>;
