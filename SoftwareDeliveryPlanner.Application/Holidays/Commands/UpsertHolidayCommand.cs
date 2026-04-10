using MediatR;

namespace SoftwareDeliveryPlanner.Application.Holidays.Commands;

public sealed record UpsertHolidayCommand(
    int Id,
    string HolidayName,
    DateTime HolidayDate,
    string HolidayType,
    string? Notes,
    bool IsNew) : IRequest<Unit>;

public sealed record DeleteHolidayCommand(int Id) : IRequest<Unit>;
