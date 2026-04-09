namespace SoftwareDeliveryPlanner.Models;

public class Holiday
{
    public int Id { get; set; }
    public string HolidayName { get; set; } = string.Empty;
    public DateTime HolidayDate { get; set; }
    public string HolidayType { get; set; } = "National";
    public string? Notes { get; set; }
}
