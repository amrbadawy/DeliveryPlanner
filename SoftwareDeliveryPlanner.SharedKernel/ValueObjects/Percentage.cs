namespace SoftwareDeliveryPlanner.SharedKernel.ValueObjects;

public readonly record struct Percentage
{
    public double Value { get; }

    private Percentage(double value)
    {
        Value = value;
    }

    public static Percentage Create(double value)
    {
        if (!TryCreate(value, out var percentage))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Percentage must be between 0 and 100.");
        }

        return percentage;
    }

    public static bool TryCreate(double value, out Percentage percentage)
    {
        percentage = default;
        if (value < 0 || value > 100)
        {
            return false;
        }

        percentage = new Percentage(value);
        return true;
    }

    public override string ToString() => Value.ToString("F1");
}
