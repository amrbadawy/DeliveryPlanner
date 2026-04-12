namespace SoftwareDeliveryPlanner.Application.Abstractions;

/// <summary>
/// Seeds initial/demo data into the database at application startup.
/// Implementations must be idempotent -- safe to call multiple times.
/// </summary>
public interface IDatabaseSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
