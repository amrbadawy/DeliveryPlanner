using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Settings.Commands;

public sealed record UpsertSettingCommand(string Key, string? Value) : IRequest<Result>;

internal sealed class UpsertSettingCommandHandler : IRequestHandler<UpsertSettingCommand, Result>
{
    private readonly ISettingsService _settingsService;

    public UpsertSettingCommandHandler(ISettingsService settingsService) => _settingsService = settingsService;

    public async Task<Result> Handle(UpsertSettingCommand request, CancellationToken cancellationToken)
    {
        await _settingsService.UpsertSettingAsync(request.Key, request.Value, cancellationToken);
        return Result.Success();
    }
}
