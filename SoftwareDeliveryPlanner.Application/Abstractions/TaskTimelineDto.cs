namespace SoftwareDeliveryPlanner.Application.Abstractions;

public sealed record TaskAssignmentDayDto(
    DateTime Date,
    string DateDisplay,
    bool IsWorkingDay,
    string StatusText,
    List<string> AssignedDevelopers);

public sealed record TaskTimelineDto(
    List<TaskAssignmentDayDto> Days);