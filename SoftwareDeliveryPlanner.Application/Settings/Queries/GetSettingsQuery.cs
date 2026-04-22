using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Settings.Queries;

public sealed record GetSettingsQuery : IRequest<Result<SettingsDto>>;

internal sealed class GetSettingsQueryHandler : IRequestHandler<GetSettingsQuery, Result<SettingsDto>>
{
    private readonly ISettingsService _settingsService;

    public GetSettingsQueryHandler(ISettingsService settingsService) => _settingsService = settingsService;

    public async Task<Result<SettingsDto>> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
    {
        var dto = await _settingsService.GetSettingsAsync(cancellationToken);
        return Result<SettingsDto>.Success(dto);
    }
}
