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
            // On Windows, back-to-back Playwright runs can leave the SQLite file briefly
            // locked by the dying previous server process.  Retry with exponential backoff
            // so the new server waits for the file to be released instead of crashing.
            const int maxAttempts = 5;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await db.Database.EnsureDeletedAsync(cancellationToken);
                    break;
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    await Task.Delay(attempt * 500, cancellationToken);
                }
            }

            await db.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        await db.Database.MigrateAsync(cancellationToken);
    }
}
