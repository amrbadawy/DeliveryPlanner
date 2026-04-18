using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Models;

public class RiskNotification
{
    public int Id { get; private set; }
    public string TaskId { get; private set; } = string.Empty;
    public string ServiceName { get; private set; } = string.Empty;
    public string PreviousRisk { get; private set; } = string.Empty;
    public string CurrentRisk { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public bool IsRead { get; private set; }

    private RiskNotification() { }

    public static RiskNotification Create(string taskId, string serviceName, string previousRisk, string currentRisk)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            throw new DomainException("TaskId is required.");

        return new RiskNotification
        {
            TaskId = taskId,
            ServiceName = serviceName,
            PreviousRisk = previousRisk,
            CurrentRisk = currentRisk,
            CreatedAt = TimeProvider.System.GetUtcNow().DateTime,
            IsRead = false
        };
    }

    public void MarkAsRead() => IsRead = true;
}
