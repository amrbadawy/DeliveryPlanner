namespace SoftwareDeliveryPlanner.Application.Abstractions;

public sealed record DashboardKpisDto(
    int TotalServices,
    double TotalEstimation,
    int ActiveResources,
    double TotalCapacity,
    DateTime? OverallFinish,
    int OnTrack,
    int AtRisk,
    int Late,
    double AvgAssigned,
    int OverallocationCount);
