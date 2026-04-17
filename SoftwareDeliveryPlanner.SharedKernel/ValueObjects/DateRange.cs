namespace SoftwareDeliveryPlanner.SharedKernel.ValueObjects;

public readonly record struct DateRange
{
    public DateTime Start { get; }
    public DateTime End { get; }

    private DateRange(DateTime start, DateTime end)
    {
        Start = start.Date;
        End = end.Date;
    }

    public static DateRange Create(DateTime start, DateTime end)
    {
        if (!TryCreate(start, end, out var range))
        {
            throw new ArgumentException("Start date must be less than or equal to end date.");
        }

        return range;
    }

    public static bool TryCreate(DateTime start, DateTime end, out DateRange range)
    {
        range = default;

        if (start.Date > end.Date)
        {
            return false;
        }

        range = new DateRange(start, end);
        return true;
    }
}
