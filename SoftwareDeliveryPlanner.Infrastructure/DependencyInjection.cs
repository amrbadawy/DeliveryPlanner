using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Data;
using SoftwareDeliveryPlanner.Infrastructure.Services;

namespace SoftwareDeliveryPlanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextFactory<PlannerDbContext>(options =>
        {
            var dbPath = ResolveDbPath(configuration);
            options.UseSqlite($"Data Source={dbPath}");
        });

        services.AddScoped<ISchedulingOrchestrator, SchedulingOrchestrator>();

        // Forward focused interfaces to the composite orchestrator instance
        services.AddScoped<ISchedulerService>(sp => sp.GetRequiredService<ISchedulingOrchestrator>());
        services.AddScoped<ITaskOrchestrator>(sp => sp.GetRequiredService<ISchedulingOrchestrator>());
        services.AddScoped<IResourceOrchestrator>(sp => sp.GetRequiredService<ISchedulingOrchestrator>());
        services.AddScoped<IAdjustmentOrchestrator>(sp => sp.GetRequiredService<ISchedulingOrchestrator>());
        services.AddScoped<IHolidayOrchestrator>(sp => sp.GetRequiredService<ISchedulingOrchestrator>());
        services.AddScoped<IPlanningQueryService>(sp => sp.GetRequiredService<ISchedulingOrchestrator>());

        return services;
    }

    private static string ResolveDbPath(IConfiguration configuration)
    {
        var dbPath = Environment.GetEnvironmentVariable("PLANNER_DB_PATH");

        if (string.IsNullOrWhiteSpace(dbPath))
        {
            dbPath = configuration["Planner:DbPath"];
        }

        if (string.IsNullOrWhiteSpace(dbPath))
        {
            dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "planner.db");
        }

        dbPath = Path.GetFullPath(dbPath);
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return dbPath;
    }
}
