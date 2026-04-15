using MediatR;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Application.Adjustments.Queries;

public sealed record GetAdjustmentsQuery : IRequest<List<Adjustment>>;
