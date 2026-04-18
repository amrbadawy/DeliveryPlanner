using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Models;

public class AuditEntry
{
    public int Id { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }

    private AuditEntry() { }

    public static AuditEntry Create(string action, string entityType, string entityId, string description, string? oldValue = null, string? newValue = null)
    {
        if (string.IsNullOrWhiteSpace(action)) throw new DomainException("Action is required.");
        if (string.IsNullOrWhiteSpace(entityType)) throw new DomainException("Entity type is required.");

        return new AuditEntry
        {
            Timestamp = TimeProvider.System.GetUtcNow().DateTime,
            Action = action.Trim(),
            EntityType = entityType.Trim(),
            EntityId = entityId?.Trim() ?? string.Empty,
            Description = description.Trim(),
            OldValue = oldValue,
            NewValue = newValue
        };
    }
}
