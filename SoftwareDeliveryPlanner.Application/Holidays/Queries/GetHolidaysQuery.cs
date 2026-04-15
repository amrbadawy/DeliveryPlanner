using MediatR;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Application.Holidays.Queries;

public sealed record GetHolidaysQuery : IRequest<List<Holiday>>;
