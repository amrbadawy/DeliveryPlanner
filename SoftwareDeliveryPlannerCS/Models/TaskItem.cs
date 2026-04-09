namespace SoftwareDeliveryPlanner.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string TaskId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public double DevEstimation { get; set; }
    public double MaxDev { get; set; } = 1.0;
    public DateTime? StrictDate { get; set; }
    public int Priority { get; set; } = 5;
    public int? SchedulingRank { get; set; }
    public double? AssignedDev { get; set; }
    public string? AssignedResourceId { get; set; }
    public DateTime? PlannedStart { get; set; }
    public DateTime? PlannedFinish { get; set; }
    public int? Duration { get; set; }
    public string Status { get; set; } = "Not Started";
    public string DeliveryRisk { get; set; } = "On Track";
    public DateTime? OverrideStart { get; set; }
    public double? OverrideDev { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
