namespace SoftwareDeliveryPlanner.Models;

public class TeamMember
{
    public int Id { get; set; }
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string Role { get; set; } = "Developer";
    public string Team { get; set; } = "Delivery";
    public double AvailabilityPct { get; set; } = 100.0;
    public double DailyCapacity { get; set; } = 1.0;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Active { get; set; } = "Yes";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
