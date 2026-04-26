using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Infrastructure.Data;
using SoftwareDeliveryPlanner.Infrastructure.Extensions;
using SoftwareDeliveryPlanner.Infrastructure.Services;

namespace SoftwareDeliveryPlanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var plannerDbPath = configuration["PLANNER_DB_PATH"];

        // Playwright E2E runs set PLANNER_DB_PATH to force an isolated SQLite file per run.
        // In normal local/dev/prod environments this value is absent and SQL Server is used.
        if (!string.IsNullOrWhiteSpace(plannerDbPath))
        {
            var sqliteConnectionString = $"Data Source={plannerDbPath}";

            services.AddDbContextFactory<PlannerDbContext>(options =>
            {
                options.UseSqlite(sqliteConnectionString);
            });

            services.AddDbContextFactory<ReadOnlyPlannerDbContext>(options =>
            {
                options.UseSqlite(sqliteConnectionString);
            });
        }
        else
        {
        var connectionString = configuration.GetConnectionString("PlannerDb")
            ?? throw new InvalidOperationException("Connection string 'PlannerDb' is not configured.");

        var readOnlyConnectionString = configuration.GetConnectionString("PlannerDbReadOnly")
            ?? connectionString; // Fallback to primary if read-only replica is not configured

        services.AddDbContextFactory<PlannerDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddDbContextFactory<ReadOnlyPlannerDbContext>(options =>
        {
            options.UseSqlServer(readOnlyConnectionString);
        });
        }

        services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
        services.AddSingleton(TimeProvider.System);

        // Scheduling engine factory — creates engine instances with their own DbContext
        services.AddSingleton<ISchedulingEngineFactory, SchedulingEngineFactory>();

        // Focused service registrations — one class per interface
        services.AddScoped<ITaskOrchestrator, TaskService>();
        services.AddScoped<IResourceOrchestrator, ResourceService>();
        services.AddScoped<IRoleOrchestrator, RoleService>();
        services.AddScoped<ILookupOrchestrator, LookupService>();
        services.AddScoped<IAdjustmentOrchestrator, AdjustmentService>();
        services.AddScoped<IHolidayOrchestrator, HolidayService>();
        services.AddScoped<ISchedulerService, SchedulerService>();
        services.AddScoped<IPlanningQueryService, PlanningQueryService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<ITaskNoteOrchestrator, TaskNoteOrchestrator>();
        services.AddScoped<INotificationOrchestrator, NotificationOrchestrator>();
        services.AddScoped<IScenarioOrchestrator, ScenarioOrchestrator>();
        services.AddScoped<IAuditService, AuditService>();

        // MediatR notification handler — lives in Infrastructure, must be registered explicitly
        // (MediatR auto-scan only covers the Application assembly)
        services.AddTransient<INotificationHandler<DomainEventNotification>, DomainEventAuditHandler>();

        // ---------------------------------------------------------------------------
        // Health checks
        // Uses EF Core DbContextFactory checks (official Microsoft package) instead of
        // raw SQL pings — catches misconfigured EF options, migration issues, and
        // connection pool exhaustion that a bare SELECT 1 would miss.
        //
        // Tags:
        //   "ready" — readiness probe (/health endpoint). DB must be up.
        //   "live"  — liveness probe (/alive endpoint). No DB dependency — intentionally absent.
        //
        // Timeout: 5s per check — prevents a hung DB connection from blocking /health indefinitely.
        // ---------------------------------------------------------------------------
        var healthChecks = services.AddHealthChecks()
            .AddDbContextCheck<PlannerDbContext>(
                name: "ef-primary",
                tags: ["ready"],
                customTestQuery: (db, ct) => db.Settings.AnyAsync(ct));

        // Only register the read-only check when its connection string is explicitly configured
        // AND distinct from the primary. In Development both point to the same _Dev database,
        // so only one check appears. In Production with a real AlwaysOn replica, both appear.
        var readOnlyCs = configuration.GetConnectionString("PlannerDbReadOnly");
        var primaryCs = configuration.GetConnectionString("PlannerDb");
        if (!string.IsNullOrEmpty(readOnlyCs) && readOnlyCs != primaryCs)
        {
            healthChecks.AddDbContextCheck<ReadOnlyPlannerDbContext>(
                name: "ef-readonly",
                tags: ["ready"],
                customTestQuery: (db, ct) => db.Settings.AnyAsync(ct));
        }

        // Health check publisher: controls how often background health checks are polled.
        // Default is 30s delay + 30s period — too slow for Aspire dashboard real-time feedback.
        services.Configure<HealthCheckPublisherOptions>(opts =>
        {
            opts.Delay = TimeSpan.FromSeconds(5);    // first check 5s after startup
            opts.Period = TimeSpan.FromSeconds(15);  // re-check every 15s
        });

        return services;
    }
}
