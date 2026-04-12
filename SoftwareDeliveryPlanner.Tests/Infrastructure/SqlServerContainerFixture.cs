using Testcontainers.MsSql;

namespace SoftwareDeliveryPlanner.Tests.Infrastructure;

/// <summary>
/// Shared xUnit collection fixture that starts a single SQL Server container
/// for the entire test run. All test classes that need a database share this
/// container, but each gets its own isolated database (unique name).
/// </summary>
public class SqlServerContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Returns a connection string pointing to a new, uniquely named database
    /// on the shared SQL Server container. Each test class should call this
    /// once in its constructor to get full isolation.
    /// </summary>
    public string CreateDatabaseConnectionString()
    {
        var dbName = $"Test_{Guid.NewGuid():N}";
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(ConnectionString)
        {
            InitialCatalog = dbName,
            TrustServerCertificate = true
        };
        return builder.ConnectionString;
    }

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
