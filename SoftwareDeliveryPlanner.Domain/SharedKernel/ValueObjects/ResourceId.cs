namespace SoftwareDeliveryPlanner.Domain.SharedKernel.ValueObjects;

public readonly record struct ResourceId
{
    public string Value { get; }

    private ResourceId(string value)
    {
        Value = value;
    }

    public static ResourceId Create(string value)
    {
        if (!TryCreate(value, out var resourceId))
        {
            throw new ArgumentException("Resource ID must be in format AAA-000.", nameof(value));
        }

        return resourceId;
    }

    public static bool TryCreate(string? value, out ResourceId resourceId)
    {
        resourceId = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToUpperInvariant();
        var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || parts[0].Length < 2 || parts[1].Length < 2)
        {
            return false;
        }

        if (!parts[0].All(char.IsLetter) || !parts[1].All(char.IsDigit))
        {
            return false;
        }

        resourceId = new ResourceId(normalized);
        return true;
    }

    public override string ToString() => Value;
}
