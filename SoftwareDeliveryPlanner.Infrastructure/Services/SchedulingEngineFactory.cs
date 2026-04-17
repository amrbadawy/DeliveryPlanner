using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
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
        return new SchedulingEngine(db, _timeProvider, ownsContext: true);
    }
}
