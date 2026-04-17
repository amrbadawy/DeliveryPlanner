using MediatR;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Adjustments.Queries;

public sealed record GetAdjustmentsQuery : IRequest<Result<List<Adjustment>>>;
