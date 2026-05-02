using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Infrastructure.Data;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

/// <summary>
/// Creates <see cref="SchedulingEngine"/> instances backed by a fresh
/// <see cref="PlannerDbContext"/>. The returned engine owns (and disposes)
/// the context.
/// </summary>
internal sealed class SchedulingEngineFactory : ISchedulingEngineFactory
{
    private readonly IDbContextFactory<PlannerDbContext> _dbFactory;
    private readonly TimeProvider _timeProvider;

    public SchedulingEngineFactory(
        IDbContextFactory<PlannerDbContext> dbFactory,
        TimeProvider timeProvider)
    {
        _dbFactory = dbFactory;
        _timeProvider = timeProvider;
    }

    public async Task<ISchedulingEngine> CreateAsync(CancellationToken cancellationToken = default)
    {
        var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        // Load settings asynchronously here so the SchedulingEngine constructor
        // stays synchronous (constructors cannot be async).
        var atRiskSetting = await db.Settings
            .FirstOrDefaultAsync(s => s.Key == DomainConstants.SettingKeys.AtRiskThreshold, cancellationToken);
        var atRiskThreshold = int.TryParse(atRiskSetting?.Value, out var t) ? t : 5;

        var weekSetting = await db.Settings
            .FirstOrDefaultAsync(s => s.Key == DomainConstants.SettingKeys.WorkingWeek, cancellationToken);
        var weekendDays = DomainConstants.WorkingWeek.GetWeekendDays(
            weekSetting?.Value ?? DomainConstants.WorkingWeek.SunThu);

        return new SchedulingEngine(db, _timeProvider, atRiskThreshold, weekendDays, ownsContext: true);
    }
}
