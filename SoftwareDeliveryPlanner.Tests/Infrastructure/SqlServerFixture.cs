using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;

namespace SoftwareDeliveryPlanner.Tests.Infrastructure;

/// <summary>
/// Shared xUnit collection fixture that provides isolated SQL Server
/// databases for integration tests.
/// <para>
/// By default connects to the local default SQL Server instance
/// (<c>Server=.;Trusted_Connection=True</c>). Override by setting the
/// <c>TEST_SQL_CONNECTION</c> environment variable to a custom
/// connection string.
/// </para>
/// <para>
/// Each test class gets its own uniquely named database via
/// <see cref="CreateDatabaseConnectionString"/>. All test databases
/// are dropped automatically after the test run completes.
/// </para>
/// </summary>
public class SqlServerFixture : IAsyncLifetime
{
    private const string EnvVarName = "TEST_SQL_CONNECTION";
    private const string DefaultConnectionString = "Server=.;Trusted_Connection=True;TrustServerCertificate=True;";

    private string _baseConnectionString = string.Empty;
    private readonly ConcurrentBag<string> _createdDatabases = new();

    /// <summary>
    /// Base connection string (master / default catalog) for the SQL Server instance.
    /// </summary>
    public string ConnectionString => _baseConnectionString;

    /// <summary>
    /// Returns a connection string pointing to a new, uniquely named database
    /// on the SQL Server instance. Each test class should call this once in
    /// its constructor to get full isolation.
    /// The database is pre-created with small initial file sizes (4 MB data /
    /// 2 MB log, 1 MB growth) so that many concurrent test databases don't
    /// exhaust limited disk space.
    /// </summary>
    public string CreateDatabaseConnectionString(string? databaseName = null)
    {
        var dbName = databaseName ?? $"Test_{Guid.NewGuid():N}";
        _createdDatabases.Add(dbName);

        PreCreateSmallDatabase(dbName);

        var builder = new SqlConnectionStringBuilder(_baseConnectionString)
        {
            InitialCatalog = dbName,
            TrustServerCertificate = true
        };
        return builder.ConnectionString;
    }

    /// <summary>
    /// Pre-creates the database with minimal file sizes so that EF Core's
    /// <c>Migrate()</c> call doesn't inherit the model database's potentially
    /// large default filegrowth (e.g. 64 MB), which would exhaust disk space
    /// when many test databases are created in parallel.
    /// </summary>
    private void PreCreateSmallDatabase(string dbName)
    {
        var masterBuilder = new SqlConnectionStringBuilder(_baseConnectionString)
        {
            InitialCatalog = "master",
            TrustServerCertificate = true
        };

        using var connection = new SqlConnection(masterBuilder.ConnectionString);
        connection.Open();

        // Query the SQL Server default data directory so we can supply
        // explicit FILENAME values (required by SQL Server 2019+).
        var dataDir = GetDefaultDataDirectory(connection);

        var mdfPath = Path.Combine(dataDir, $"{dbName}.mdf");
        var ldfPath = Path.Combine(dataDir, $"{dbName}_log.ldf");

        // dbName is a GUID — no injection risk.
        var sql = $"""
            CREATE DATABASE [{dbName}]
            ON PRIMARY (
                NAME = N'{dbName}',
                FILENAME = N'{mdfPath}',
                SIZE = 4MB,
                FILEGROWTH = 1MB
            )
            LOG ON (
                NAME = N'{dbName}_log',
                FILENAME = N'{ldfPath}',
                SIZE = 2MB,
                FILEGROWTH = 1MB
            )
            """;

        using var cmd = new SqlCommand(sql, connection);
        cmd.CommandTimeout = 30;
        cmd.ExecuteNonQuery();
    }

    private static string GetDefaultDataDirectory(SqlConnection connection)
    {
        using var cmd = new SqlCommand(
            "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(260))",
            connection);
        var result = cmd.ExecuteScalar() as string;
        return result?.TrimEnd('\\') ?? throw new InvalidOperationException(
            "Could not determine SQL Server default data directory.");
    }

    public async Task InitializeAsync()
    {
        var envConnectionString = Environment.GetEnvironmentVariable(EnvVarName);
        _baseConnectionString = !string.IsNullOrWhiteSpace(envConnectionString)
            ? envConnectionString
            : DefaultConnectionString;

        // Clean up any stale Test_ databases left over from previous
        // crashed or timed-out test runs that never reached DisposeAsync.
        await DropStaleTestDatabasesAsync();
    }

    public async Task DisposeAsync()
    {
        if (!_createdDatabases.IsEmpty)
        {
            await DropTestDatabasesAsync();
        }
    }

    private async Task DropTestDatabasesAsync()
    {
        var masterBuilder = new SqlConnectionStringBuilder(_baseConnectionString)
        {
            InitialCatalog = "master",
            TrustServerCertificate = true
        };

        await using var connection = new SqlConnection(masterBuilder.ConnectionString);
        await connection.OpenAsync();

        foreach (var dbName in _createdDatabases)
        {
            try
            {
                var sql = $"""
                    IF DB_ID(N'{dbName}') IS NOT NULL
                    BEGIN
                        ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE [{dbName}];
                    END
                    """;

                await using var cmd = new SqlCommand(sql, connection);
                cmd.CommandTimeout = 30;
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Best-effort cleanup — don't fail the test run for cleanup issues.
            }
        }
    }

    /// <summary>
    /// Drops any <c>Test_*</c> databases left over from previous test runs
    /// that crashed or timed out before <see cref="DisposeAsync"/> could execute.
    /// </summary>
    private async Task DropStaleTestDatabasesAsync()
    {
        var masterBuilder = new SqlConnectionStringBuilder(_baseConnectionString)
        {
            InitialCatalog = "master",
            TrustServerCertificate = true
        };

        try
        {
            await using var connection = new SqlConnection(masterBuilder.ConnectionString);
            await connection.OpenAsync();

            var sql = """
                DECLARE @sql NVARCHAR(MAX) = N'';
                SELECT @sql += N'ALTER DATABASE [' + name + N'] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [' + name + N']; '
                FROM sys.databases
                WHERE name LIKE 'Test_%';
                EXEC sp_executesql @sql;
                """;

            await using var cmd = new SqlCommand(sql, connection);
            cmd.CommandTimeout = 60;
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best-effort — if cleanup fails, tests will still try to run.
        }
    }
}
