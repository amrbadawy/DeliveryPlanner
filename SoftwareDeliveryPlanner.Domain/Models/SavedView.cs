using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Models;

/// <summary>
/// A user-named bookmark of the task filter sidebar's current selections.
/// Stores filter dimension selections + per-task pin/hide overrides as a JSON payload.
///
/// <para>
/// <b>OwnerKey</b> identifies the owning principal when authentication is configured.
/// When auth is absent (current state of the app) all callers store/retrieve with
/// OwnerKey = null and views are effectively global. The schema is forward-compatible
/// with a future per-user model: the application layer simply scopes queries by
/// OwnerKey once an authentication scheme is added.
/// </para>
/// </summary>
public class SavedView
{
    public int Id { get; private set; }

    /// <summary>Human-readable view name. Unique per (OwnerKey, PageKey).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>"tasks" or "gantt" — matches TaskFilterState page keys.</summary>
    public string PageKey { get; private set; } = string.Empty;

    /// <summary>Owner principal identifier. Null = global / shared.</summary>
    public string? OwnerKey { get; private set; }

    /// <summary>Serialized SavedViewPayload (JSON). Stored as-is for forward compatibility.</summary>
    public string PayloadJson { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private SavedView() { }

    public static SavedView Create(string name, string pageKey, string payloadJson, string? ownerKey = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Saved view name must not be empty.");
        if (name.Length > 100)
            throw new DomainException("Saved view name must be 100 characters or fewer.");
        if (string.IsNullOrWhiteSpace(pageKey))
            throw new DomainException("Page key is required.");
        if (string.IsNullOrWhiteSpace(payloadJson))
            throw new DomainException("Payload must not be empty.");

        var now = TimeProvider.System.GetUtcNow().DateTime;
        return new SavedView
        {
            Name = name.Trim(),
            PageKey = pageKey.Trim().ToLowerInvariant(),
            OwnerKey = string.IsNullOrWhiteSpace(ownerKey) ? null : ownerKey.Trim(),
            PayloadJson = payloadJson,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdatePayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            throw new DomainException("Payload must not be empty.");
        PayloadJson = payloadJson;
        UpdatedAt = TimeProvider.System.GetUtcNow().DateTime;
    }
}
