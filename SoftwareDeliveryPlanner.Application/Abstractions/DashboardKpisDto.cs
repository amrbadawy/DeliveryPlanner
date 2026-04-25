namespace SoftwareDeliveryPlanner.Application.Abstractions;

public sealed record DashboardKpisDto(
    int TotalServices,
    double TotalEstimation,
    int ActiveResources,
    double TotalCapacity,
    DateTime? EarliestStart,
    DateTime? OverallFinish,
    int OnTrack,
    int AtRisk,
    int Late,
    int Unscheduled,
    double AvgAssigned,
    int OverallocationCount);
