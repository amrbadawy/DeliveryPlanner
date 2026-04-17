using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Holidays.Commands;

internal sealed class CopyHolidaysToYearCommandHandler : IRequestHandler<CopyHolidaysToYearCommand, Result<int>>
{
    private readonly IHolidayOrchestrator _orchestrator;

    public CopyHolidaysToYearCommandHandler(IHolidayOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<int>> Handle(CopyHolidaysToYearCommand request, CancellationToken cancellationToken)
    {
        var copied = await _orchestrator.CopyHolidaysToYearAsync(
            request.SourceYear, request.TargetYear, cancellationToken);

        return Result<int>.Success(copied);
    }
}
