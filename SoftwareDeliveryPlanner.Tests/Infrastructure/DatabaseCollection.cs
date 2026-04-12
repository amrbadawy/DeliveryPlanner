namespace SoftwareDeliveryPlanner.Tests.Infrastructure;

/// <summary>
/// xUnit collection definition that ensures the SQL Server container
/// is shared across all test classes in this collection.
/// </summary>
[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<SqlServerContainerFixture>
{
    public const string Name = "Database";
}
