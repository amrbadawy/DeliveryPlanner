// Software Delivery Planner — Aspire AppHost
//
// This is the orchestration entry point. It is completely separate from the
// main solution (SoftwareDeliveryPlanner.slnx) and has no impact on the
// Web project when running in standalone mode (dotnet run).
//
// Usage:
//   aspire run      — launches dashboard + Web app with full observability
//   dotnet run      — same, using .NET CLI directly from this folder
//
// The SQL Server instance is modelled as an external connection string resource
// (no Docker container). Aspire reads it from appsettings.json in this project
// and injects it into the Web project via the standard ConnectionStrings config key.

var builder = DistributedApplication.CreateBuilder(args);

// External SQL Server resource — uses existing local SQL Server, no container needed.
// Connection string is read from this project's appsettings.json.
var plannerDb = builder.AddConnectionString("PlannerDb");

builder.AddProject<Projects.SoftwareDeliveryPlanner_Web>("web")
    .WithReference(plannerDb);

await builder.Build().RunAsync();
