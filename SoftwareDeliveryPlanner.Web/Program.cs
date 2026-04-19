using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SoftwareDeliveryPlanner.Application;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Infrastructure;
using SoftwareDeliveryPlanner.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Observability: OpenTelemetry (traces, metrics, structured logs)
// Instrumentation is always active; export only when a collector is available.
// Aspire sets OTEL_EXPORTER_OTLP_ENDPOINT automatically when orchestrating.
// In standalone mode the OTLP exporter is not registered — zero export overhead.
// ---------------------------------------------------------------------------
const string serviceName = "SoftwareDeliveryPlanner";

// Export to OTLP only when a collector endpoint is present (e.g. Aspire AppHost).
// This env var is automatically injected by the Aspire AppHost during orchestration.
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
var hasOtlpCollector = !string.IsNullOrWhiteSpace(otlpEndpoint);

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("Microsoft.EntityFrameworkCore");  // EF Core built-in activity source

        if (hasOtlpCollector)
            tracing.AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();

        if (hasOtlpCollector)
            metrics.AddOtlpExporter();
    });

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeScopes = true;
    logging.IncludeFormattedMessage = true;

    if (hasOtlpCollector)
        logging.AddOtlpExporter();
});

// ---------------------------------------------------------------------------
// Application services
// ---------------------------------------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddApplication();
builder.Services.AddScoped<SoftwareDeliveryPlanner.Web.Services.AutoScheduleService>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Apply pending migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var migrator = scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>();
    await migrator.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    await seeder.SeedAsync();
}

// ---------------------------------------------------------------------------
// Health endpoints
//   /health  — readiness (includes SQL Server check)
//   /alive   — liveness (process is up, no dependency checks)
// ---------------------------------------------------------------------------
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true   // include all registered checks
});
app.MapHealthChecks("/alive", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")   // liveness-only subset
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
