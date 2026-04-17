namespace SoftwareDeliveryPlanner.Application.Abstractions;

/// <summary>Semantic status for a timeline day, mapped to colors in the UI layer.</summary>
public enum TimelineDayStatus
{
    Free,
    Weekend,
    Holiday,
    Adjustment,
    Working
}

public sealed record TimelineDayDto(
    DateTime Date,
    string DateDisplay,
    TimelineDayStatus Status,
    string StatusText,
    string? TaskId = null,
    string? TaskName = null);

public sealed record TimelineDataDto(
    List<TimelineDayDto> Days);
