using MediatR;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Holidays.Commands;

public sealed record UpsertHolidayCommand(
    int Id,
    string HolidayName,
    DateTime StartDate,
    DateTime EndDate,
    string HolidayType,
    string? Notes,
    bool IsNew) : IRequest<Result>;

public sealed record DeleteHolidayCommand(int Id) : IRequest<Result>;
