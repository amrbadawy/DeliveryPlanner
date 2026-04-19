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

// External SQL Server resources — uses existing local SQL Server, no container needed.
// Connection strings are read from this project's appsettings.json and injected into
// the Web project as environment variables (ConnectionStrings__PlannerDb etc.).
var plannerDb = builder.AddConnectionString("PlannerDb");
var plannerDbReadOnly = builder.AddConnectionString("PlannerDbReadOnly");

builder.AddProject<Projects.SoftwareDeliveryPlanner_Web>("web")
    .WithReference(plannerDb)
    .WithReference(plannerDbReadOnly);

await builder.Build().RunAsync();
