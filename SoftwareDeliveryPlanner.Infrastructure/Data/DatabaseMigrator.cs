using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;

namespace SoftwareDeliveryPlanner.Infrastructure.Data;

internal sealed class DatabaseMigrator : IDatabaseMigrator
{
    private readonly IDbContextFactory<PlannerDbContext> _dbFactory;

    public DatabaseMigrator(IDbContextFactory<PlannerDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        // Playwright E2E runs with an isolated SQLite file (PLANNER_DB_PATH).
        // Use EnsureCreated instead of Migrate because SQL Server migrations are not portable to SQLite.
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PLANNER_DB_PATH")))
        {
            await db.Database.EnsureDeletedAsync(cancellationToken);
            await db.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        await db.Database.MigrateAsync(cancellationToken);
    }
}
