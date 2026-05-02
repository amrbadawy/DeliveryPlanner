using System.Text.Json;

namespace SoftwareDeliveryPlanner.Web.Services;

/// <summary>
/// Compares two saved-view payload JSON strings structurally, treating
/// arrays as sorted multisets and ignoring unknown fields (forward-compat).
/// Used by TaskFilterSidebar to detect filter drift from the applied view.
/// </summary>
public static class SavedViewPayloadComparer
{
    /// <summary>Known fields in SavedViewPayload (camelCase, as serialized by JsonSerializerDefaults.Web).</summary>
    private static readonly string[] KnownFields =
    [
        "searchTerm",
        "statuses",
        "risks",
        "priorityBuckets",
        "phases",
        "roles",
        "dependencyStates",
        "pinnedTaskIds",
        "hiddenTaskIds",
    ];

    /// <summary>
    /// Returns true when both payloads represent the same filter selection.
    /// Null/missing fields are treated as empty. Array order is ignored (sorted-multiset
    /// comparison). Unknown keys are ignored for forward-compatibility.
    /// </summary>
    public static bool AreEqual(string? payloadA, string? payloadB)
    {
        // Null payloads are never considered equal — defensive: a missing default
        // payload should always count as drift, never silently match.
        if (payloadA is null || payloadB is null) return false;
        if (ReferenceEquals(payloadA, payloadB)) return true;
        if (payloadA == payloadB) return true;

        try
        {
            using var docA = JsonDocument.Parse(payloadA);
            using var docB = JsonDocument.Parse(payloadB);
            return StructurallyEqual(docA.RootElement, docB.RootElement);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool StructurallyEqual(JsonElement a, JsonElement b)
    {
        foreach (var field in KnownFields)
        {
            var listA = GetSortedValues(a, field);
            var listB = GetSortedValues(b, field);
            if (!listA.SequenceEqual(listB, StringComparer.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    private static List<string> GetSortedValues(JsonElement root, string field)
    {
        if (!root.TryGetProperty(field, out var el))
            return [];

        return el.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => [],
            JsonValueKind.String => ToSingletonList(el.GetString()),
            JsonValueKind.Array => el.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString() ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            _ => [],
        };
    }

    private static List<string> ToSingletonList(string? value) =>
        string.IsNullOrWhiteSpace(value) ? [] : [value];
}
