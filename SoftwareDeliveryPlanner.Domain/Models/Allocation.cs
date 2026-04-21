using SoftwareDeliveryPlanner.Domain;

namespace SoftwareDeliveryPlanner.Domain.Models;

/// <summary>
/// Records a single resource-day allocation: "Resource X worked Y hours on Task Z (role phase R) on Date D."
/// Created by the scheduling engine during the allocation pass.
/// </summary>
public class Allocation
{
    public int Id { get; set; }
    public string AllocationId { get; set; } = string.Empty;
    public string TaskId { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int DateKey { get; set; }
    public DateTime CalendarDate { get; set; }
    public int? SchedRank { get; set; }
    public double HoursAllocated { get; set; }
    public double CumulativeEffort { get; set; }
    public bool IsComplete { get; set; }
    public string ServiceStatus { get; set; } = DomainConstants.TaskStatus.NotStarted;

    // Navigation property
    public TaskItem? Task { get; set; }
}
