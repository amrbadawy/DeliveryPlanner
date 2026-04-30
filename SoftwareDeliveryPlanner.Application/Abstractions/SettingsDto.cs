namespace SoftwareDeliveryPlanner.Application.Abstractions;

public sealed record SettingsDto(
    string? SchedulingStrategy,
    string? BaselineDate,
    string? GlobalWorkingWeek,
    string? PlanStartDate,
    int AtRiskThreshold,
    string? WeekNumbering = null,
    string? GanttZoomLevel = null);
