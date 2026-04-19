using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Infrastructure.Data;
using SoftwareDeliveryPlanner.Infrastructure.Extensions;
using SoftwareDeliveryPlanner.Infrastructure.Services;

namespace SoftwareDeliveryPlanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
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

        services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
        services.AddSingleton(TimeProvider.System);

        // Scheduling engine factory — creates engine instances with their own DbContext
        services.AddSingleton<ISchedulingEngineFactory, SchedulingEngineFactory>();

        // Focused service registrations — one class per interface
        services.AddScoped<ITaskOrchestrator, TaskService>();
        services.AddScoped<IResourceOrchestrator, ResourceService>();
        services.AddScoped<IAdjustmentOrchestrator, AdjustmentService>();
        services.AddScoped<IHolidayOrchestrator, HolidayService>();
        services.AddScoped<ISchedulerService, SchedulerService>();
        services.AddScoped<IPlanningQueryService, PlanningQueryService>();
        services.AddScoped<ITaskNoteOrchestrator, TaskNoteOrchestrator>();
        services.AddScoped<INotificationOrchestrator, NotificationOrchestrator>();
        services.AddScoped<IScenarioOrchestrator, ScenarioOrchestrator>();
        services.AddScoped<IAuditService, AuditService>();

        // MediatR notification handler — lives in Infrastructure, must be registered explicitly
        // (MediatR auto-scan only covers the Application assembly)
        services.AddTransient<INotificationHandler<DomainEventNotification>, DomainEventAuditHandler>();

        // Health checks
        // "sqlserver" check is tagged "ready" (readiness probe) — not "live" (liveness probe).
        // This allows /alive to return healthy even when the DB is briefly unreachable.
        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: connectionString,
                name: "sqlserver",
                tags: ["ready"]);

        return services;
    }
}
