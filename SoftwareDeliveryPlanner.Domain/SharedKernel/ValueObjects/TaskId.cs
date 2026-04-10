namespace SoftwareDeliveryPlanner.Domain.SharedKernel.ValueObjects;

public readonly record struct TaskId
{
    public string Value { get; }

    private TaskId(string value)
    {
        Value = value;
    }

    public static TaskId Create(string value)
    {
        if (!TryCreate(value, out var taskId))
        {
            throw new ArgumentException("Task ID must be in format AAA-000.", nameof(value));
        }

        return taskId;
    }

    public static bool TryCreate(string? value, out TaskId taskId)
    {
        taskId = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length < 5)
        {
            return false;
        }

        var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || parts[0].Length < 2 || parts[1].Length < 2)
        {
            return false;
        }

        if (!parts[0].All(char.IsLetter) || !parts[1].All(char.IsDigit))
        {
            return false;
        }

        taskId = new TaskId(normalized);
        return true;
    }

    public override string ToString() => Value;
}
