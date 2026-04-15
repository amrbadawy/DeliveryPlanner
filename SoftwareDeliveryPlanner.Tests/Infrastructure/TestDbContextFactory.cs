using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Infrastructure.Data;

namespace SoftwareDeliveryPlanner.Tests.Infrastructure;

/// <summary>
/// IDbContextFactory implementation backed by SQL Server for test use.
/// Reusable across test files without conflicting file-scoped declarations.
/// </summary>
internal sealed class TestDbContextFactory : IDbContextFactory<PlannerDbContext>
{
    private readonly DbContextOptions<PlannerDbContext> _options;

    internal TestDbContextFactory(DbContextOptions<PlannerDbContext> options)
        => _options = options;

    public PlannerDbContext CreateDbContext() => new(_options);

    public Task<PlannerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new PlannerDbContext(_options));
    }
}
