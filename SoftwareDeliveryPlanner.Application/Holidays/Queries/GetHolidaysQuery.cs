using MediatR;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Holidays.Queries;

public sealed record GetHolidaysQuery : IRequest<Result<List<Holiday>>>;
