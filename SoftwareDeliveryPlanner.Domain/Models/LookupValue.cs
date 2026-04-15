namespace SoftwareDeliveryPlanner.Domain.Models;

/// <summary>
/// Generic lookup entity stored in the database. All categorical values
/// (statuses, types, roles, etc.) are persisted as lookup rows rather than enums.
/// </summary>
public class LookupValue
{
    public int Id { get; set; }

    /// <summary>Groups related lookups (e.g. "TaskStatus", "DeliveryRisk").</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>The value stored in referencing entities. Matches <see cref="Domain.DomainConstants"/> values.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable label shown in the UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Controls ordering in dropdowns and lists.</summary>
    public int SortOrder { get; set; }

    /// <summary>When false, the value is hidden from new selections but preserved in existing data.</summary>
    public bool IsActive { get; set; } = true;
}
