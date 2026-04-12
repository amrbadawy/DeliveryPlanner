using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Data;
using SoftwareDeliveryPlanner.Infrastructure.Data;
using SoftwareDeliveryPlanner.Infrastructure.Services;

namespace SoftwareDeliveryPlanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PlannerDb")
            ?? throw new InvalidOperationException("Connection string 'PlannerDb' is not configured.");

        services.AddDbContextFactory<PlannerDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
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
}
