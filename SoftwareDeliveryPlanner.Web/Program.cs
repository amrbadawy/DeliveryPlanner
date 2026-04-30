using System.Globalization;
using System.Reflection;
using HealthChecks.UI.Client;
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
// Culture: force invariant culture for CSS numeric formatting.
// Prevents locale-specific decimal separators (e.g. "56,5%") from producing
// invalid CSS values in server-side rendered Blazor components.
// ---------------------------------------------------------------------------
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

// ---------------------------------------------------------------------------
// Observability: OpenTelemetry (traces, metrics, structured logs)
// Instrumentation is always active; export only when a collector is available.
// Aspire sets OTEL_EXPORTER_OTLP_ENDPOINT automatically when orchestrating.
// In standalone mode the OTLP exporter is not registered — zero export overhead.
// ---------------------------------------------------------------------------
const string serviceName = "SoftwareDeliveryPlanner";
var serviceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

// Export to OTLP only when a collector endpoint is present (e.g. Aspire AppHost).
// This env var is automatically injected by the Aspire AppHost during orchestration.
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
var hasOtlpCollector = !string.IsNullOrWhiteSpace(otlpEndpoint);

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService(serviceName, serviceVersion: serviceVersion)
        .AddAttributes(new Dictionary<string, object>
        {
            // Environment tag flows through to every trace, metric, and log —
            // essential for filtering in production APM (Datadog, Grafana, Azure Monitor).
            ["deployment.environment"] = builder.Environment.EnvironmentName
        }))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("Microsoft.EntityFrameworkCore")          // EF Core built-in activity source
            .AddSource("SoftwareDeliveryPlanner.SchedulingEngine"); // Scheduling engine spans

        if (hasOtlpCollector)
            tracing.AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("SoftwareDeliveryPlanner.Scheduling"); // Scheduling run metrics

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
builder.Services.AddScoped<SoftwareDeliveryPlanner.Web.Services.TaskFilterState>();
builder.Services.AddScoped<SoftwareDeliveryPlanner.Web.Services.ResourceFilterState>();
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
//
//   /alive         — liveness  : process is up, no dependency checks.
//                    Restricted to localhost — only Aspire/process manager needs it.
//
//   /health        — readiness : includes all registered checks (EF Core / SQL Server).
//                    Restricted to localhost — only Aspire dashboard needs it.
//
//   /health/detail — full JSON : structured response for external monitoring agents
//                    (Datadog, Azure Monitor, uptime checkers). Publicly accessible.
// ---------------------------------------------------------------------------
app.MapHealthChecks("/alive", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
}).RequireHost("localhost", "localhost:*", "127.0.0.1", "127.0.0.1:*");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true
}).RequireHost("localhost", "localhost:*", "127.0.0.1", "127.0.0.1:*");

app.MapHealthChecks("/health/detail", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse  // structured JSON
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
