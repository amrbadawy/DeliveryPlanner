using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Infrastructure.Data;

namespace SoftwareDeliveryPlanner.Tests.Infrastructure;

/// <summary>
/// IDbContextFactory implementation for ReadOnlyPlannerDbContext.
/// Points to the same SQL Server test database as the read-write factory.
/// </summary>
internal sealed class TestReadOnlyDbContextFactory : IDbContextFactory<ReadOnlyPlannerDbContext>
{
    private readonly DbContextOptions<ReadOnlyPlannerDbContext> _options;

    internal TestReadOnlyDbContextFactory(string connectionString)
    {
        _options = new DbContextOptionsBuilder<ReadOnlyPlannerDbContext>()
            .UseSqlServer(connectionString)
            .Options;
    }

    public ReadOnlyPlannerDbContext CreateDbContext() => new(_options);

    public Task<ReadOnlyPlannerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ReadOnlyPlannerDbContext(_options));
    }
}
