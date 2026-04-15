using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SoftwareDeliveryPlanner.Infrastructure.Data;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> CLI tooling to construct the
/// DbContext when generating migrations. This factory is never used at runtime.
/// </summary>
internal class PlannerDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PlannerDbContext>
{
    PlannerDbContext IDesignTimeDbContextFactory<PlannerDbContext>.CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PlannerDbContext>();

        // This connection string is only used for migration generation/scaffolding.
        // It does not need to point to a running SQL Server instance for migration
        // creation, but it must be a valid SQL Server connection string format.
        optionsBuilder.UseSqlServer(
            "Server=.;Database=SoftwareDeliveryPlanner_Design;Trusted_Connection=True;TrustServerCertificate=True;");

        return new PlannerDbContext(optionsBuilder.Options);
    }
}
