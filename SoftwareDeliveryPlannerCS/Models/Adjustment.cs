namespace SoftwareDeliveryPlanner.Models;

public class Adjustment
{
    public int Id { get; set; }
    public string ResourceId { get; set; } = string.Empty;
    public DateTime AdjStart { get; set; }
    public DateTime AdjEnd { get; set; }
    public double AvailabilityPct { get; set; }
    public string AdjType { get; set; } = "Other";
    public string? Notes { get; set; }
    
    // Navigation property
    public TeamMember? Resource { get; set; }
}
