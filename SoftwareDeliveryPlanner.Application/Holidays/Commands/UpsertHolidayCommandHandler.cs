using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Holidays.Commands;

internal sealed class UpsertHolidayCommandHandler : IRequestHandler<UpsertHolidayCommand, Result>
{
    private readonly IHolidayOrchestrator _orchestrator;

    public UpsertHolidayCommandHandler(IHolidayOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result> Handle(UpsertHolidayCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.UpsertHolidayAsync(
            request.Id, request.HolidayName, request.StartDate,
            request.EndDate, request.HolidayType, request.Notes,
            request.IsNew, cancellationToken);

        return Result.Success();
    }
}

internal sealed class DeleteHolidayCommandHandler : IRequestHandler<DeleteHolidayCommand, Result>
{
    private readonly IHolidayOrchestrator _orchestrator;

    public DeleteHolidayCommandHandler(IHolidayOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result> Handle(DeleteHolidayCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.DeleteHolidayAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
