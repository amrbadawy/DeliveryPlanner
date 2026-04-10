using MediatR;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Application.Holidays.Queries;

public sealed record GetHolidaysQuery : IRequest<List<Holiday>>;
