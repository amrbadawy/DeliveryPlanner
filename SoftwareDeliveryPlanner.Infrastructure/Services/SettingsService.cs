using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Data;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal sealed class SettingsService : ISettingsService
{
    private readonly IDbContextFactory<PlannerDbContext> _dbFactory;

    public SettingsService(IDbContextFactory<PlannerDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<SettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var settings = await db.Settings.ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        settings.TryGetValue(DomainConstants.SettingKeys.SchedulingStrategy, out var strategy);
        settings.TryGetValue(DomainConstants.SettingKeys.BaselineDate, out var baselineDate);
        settings.TryGetValue(DomainConstants.SettingKeys.WorkingWeek, out var workingWeek);
        settings.TryGetValue(DomainConstants.SettingKeys.PlanStartDate, out var planStartDate);
        settings.TryGetValue(DomainConstants.SettingKeys.AtRiskThreshold, out var atRiskThresholdStr);
        settings.TryGetValue(DomainConstants.SettingKeys.WeekNumbering, out var weekNumbering);
        settings.TryGetValue(DomainConstants.SettingKeys.GanttZoomLevel, out var ganttZoomLevel);
        var atRiskThreshold = int.TryParse(atRiskThresholdStr, out var threshold) ? threshold : 5;

        return new SettingsDto(strategy, baselineDate, workingWeek, planStartDate, atRiskThreshold, weekNumbering, ganttZoomLevel);
    }

    public async Task UpsertSettingAsync(string key, string? value, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var setting = await db.Settings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (setting is not null)
        {
            if (value is null)
                db.Settings.Remove(setting);
            else
                setting.Value = value;
        }
        else if (value is not null)
        {
            db.Settings.Add(new Setting { Key = key, Value = value });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
