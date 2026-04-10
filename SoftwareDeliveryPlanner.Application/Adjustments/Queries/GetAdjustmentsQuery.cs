using MediatR;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Application.Adjustments.Queries;

public sealed record GetAdjustmentsQuery : IRequest<List<Adjustment>>;
