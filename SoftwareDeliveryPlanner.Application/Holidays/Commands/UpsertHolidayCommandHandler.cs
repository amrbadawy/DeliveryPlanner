using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Application.Holidays.Commands;

internal sealed class UpsertHolidayCommandHandler : IRequestHandler<UpsertHolidayCommand, Unit>
{
    private readonly IHolidayOrchestrator _orchestrator;

    public UpsertHolidayCommandHandler(IHolidayOrchestrator orchestrator)
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

internal sealed class DeleteHolidayCommandHandler : IRequestHandler<DeleteHolidayCommand, Unit>
{
    private readonly IHolidayOrchestrator _orchestrator;

    public DeleteHolidayCommandHandler(IHolidayOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Unit> Handle(DeleteHolidayCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.DeleteHolidayAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
