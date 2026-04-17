using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.DeliveryInsights.Queries;

public sealed record GetDashboardKpisQuery : IRequest<Result<DashboardKpisDto>>;
