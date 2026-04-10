namespace SoftwareDeliveryPlanner.Application.Abstractions;

public interface ISchedulingOrchestrator
{
    Task<string> RunSchedulerAsync(CancellationToken cancellationToken = default);
    Task<DashboardKpisDto> GetDashboardKpisAsync(CancellationToken cancellationToken = default);
}
