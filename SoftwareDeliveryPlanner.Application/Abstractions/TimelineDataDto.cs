namespace SoftwareDeliveryPlanner.Application.Abstractions;

public sealed record TimelineDayDto(
    DateTime Date,
    string DateDisplay,
    string BackgroundColor,
    string StatusText);

public sealed record TimelineDataDto(
    List<TimelineDayDto> Days);
