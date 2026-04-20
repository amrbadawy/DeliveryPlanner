using SoftwareDeliveryPlanner.Domain;

namespace SoftwareDeliveryPlanner.Domain.Models;

public class Allocation
{
    public int Id { get; set; }
    public string AllocationId { get; set; } = string.Empty;
    public string TaskId { get; set; } = string.Empty;
    public int DateKey { get; set; }
    public DateTime CalendarDate { get; set; }
    public int? SchedRank { get; set; }
    public double? MaxResource { get; set; }
    public double? AvailableCapacity { get; set; }
    public double AssignedResource { get; set; }
    public double CumulativeEffort { get; set; }
    public bool IsComplete { get; set; }
    public string ServiceStatus { get; set; } = DomainConstants.TaskStatus.NotStarted;

    // Navigation property
    public TaskItem? Task { get; set; }
}
