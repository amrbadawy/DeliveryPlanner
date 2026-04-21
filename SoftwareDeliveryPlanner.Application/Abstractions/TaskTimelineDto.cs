namespace SoftwareDeliveryPlanner.Application.Abstractions;

public sealed record AssignedResourceInfo(
    string ResourceId,
    string ResourceName,
    string Role);

public sealed record TaskAssignmentDayDto(
    DateTime Date,
    string DateDisplay,
    bool IsWorkingDay,
    string StatusText,
    List<AssignedResourceInfo> AssignedResources);

public sealed record TaskTimelineDto(
    List<TaskAssignmentDayDto> Days);
