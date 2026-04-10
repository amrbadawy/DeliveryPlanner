using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Application.Holidays.Commands;

public sealed class UpsertHolidayCommandHandler : IRequestHandler<UpsertHolidayCommand, Unit>
{
    private readonly ISchedulingOrchestrator _orchestrator;

    public UpsertHolidayCommandHandler(ISchedulingOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Unit> Handle(UpsertHolidayCommand request, CancellationToken cancellationToken)
    {
        var holiday = Holiday.Create(
            request.HolidayName,
            request.StartDate,
            request.EndDate,
            request.HolidayType,
            request.Notes);

        holiday.Id = request.Id;

        await _orchestrator.UpsertHolidayAsync(holiday, request.IsNew, cancellationToken);
        return Unit.Value;
    }
}

public sealed class DeleteHolidayCommandHandler : IRequestHandler<DeleteHolidayCommand, Unit>
{
    private readonly ISchedulingOrchestrator _orchestrator;

    public DeleteHolidayCommandHandler(ISchedulingOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Unit> Handle(DeleteHolidayCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.DeleteHolidayAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
