using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace SoftwareDeliveryPlanner.Tests.Infrastructure;

/// <summary>
/// Shared xUnit collection fixture that provides a SQL Server connection
/// for integration tests. Supports two modes:
/// <list type="bullet">
///   <item>
///     <b>Local SQL Server</b> — set the <c>TEST_SQL_CONNECTION</c>
///     environment variable to a valid connection string (e.g.
///     <c>Server=.;Trusted_Connection=True;TrustServerCertificate=True;</c>).
///     Each test class gets an isolated database with a unique name.
///     All test databases are dropped automatically after the run.
///   </item>
///   <item>
///     <b>Testcontainers (default)</b> — when the env var is not set,
///     a Docker-based SQL Server 2022 container is started automatically.
///   </item>
/// </list>
/// </summary>
public class SqlServerContainerFixture : IAsyncLifetime
{
    private const string EnvVarName = "TEST_SQL_CONNECTION";

    private MsSqlContainer? _container;
    private string _baseConnectionString = string.Empty;
    private readonly ConcurrentBag<string> _createdDatabases = new();

    /// <summary>
    /// True when the fixture is using a local SQL Server instead of a container.
    /// </summary>
    public bool IsLocalServer { get; private set; }

    /// <summary>
    /// Base connection string (master / default catalog) for the SQL Server instance.
    /// </summary>
    public string ConnectionString => _baseConnectionString;

    /// <summary>
    /// Returns a connection string pointing to a new, uniquely named database
    /// on the SQL Server instance. Each test class should call this once in
    /// its constructor to get full isolation.
    /// When running against a local server the database is pre-created with
    /// small initial file sizes (4 MB data / 2 MB log, 1 MB growth) so that
    /// 39+ concurrent test databases don't exhaust limited disk space.
    /// </summary>
    public string CreateDatabaseConnectionString(string? databaseName = null)
    {
        var dbName = databaseName ?? $"Test_{Guid.NewGuid():N}";
        _createdDatabases.Add(dbName);

        if (IsLocalServer)
        {
            PreCreateSmallDatabase(dbName);
        }

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
        // SERVERPROPERTY('InstanceDefaultDataPath') returns the default
        // data directory for the SQL Server instance (e.g.
        // C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\).
        using var cmd = new SqlCommand(
            "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(260))",
            connection);
        var result = cmd.ExecuteScalar() as string;
        return result?.TrimEnd('\\') ?? throw new InvalidOperationException(
            "Could not determine SQL Server default data directory.");
    }

    public async Task InitializeAsync()
    {
        var localConnectionString = Environment.GetEnvironmentVariable(EnvVarName);

        if (!string.IsNullOrWhiteSpace(localConnectionString))
        {
            // Use the local SQL Server — no container needed.
            IsLocalServer = true;
            _baseConnectionString = localConnectionString;

            // Clean up any stale Test_ databases left over from previous
            // crashed or timed-out test runs that never reached DisposeAsync.
            await DropStaleTestDatabasesAsync();
        }
        else
        {
            // Fall back to Testcontainers.
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();

            await _container.StartAsync();
            _baseConnectionString = _container.GetConnectionString();
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            // Container mode: the container itself is destroyed, taking all DBs with it.
            await _container.DisposeAsync();
        }
        else if (IsLocalServer && !_createdDatabases.IsEmpty)
        {
            // Local mode: clean up all test databases we created.
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
