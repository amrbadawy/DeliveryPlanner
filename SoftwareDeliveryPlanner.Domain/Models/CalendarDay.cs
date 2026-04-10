namespace SoftwareDeliveryPlanner.Models;

public class CalendarDay
{
    public int Id { get; set; }
    public int DateKey { get; set; }
    public DateTime CalendarDate { get; set; }
    public string? DayName { get; set; }
    public bool IsWorkingDay { get; set; }
    public bool IsHoliday { get; set; }
    public string? HolidayName { get; set; }
    public double BaseCapacity { get; set; }
    public double AdjCapacity { get; set; }
    public double EffectiveCapacity { get; set; }
    public double ReservedCapacity { get; set; }
    public double RemainingCapacity { get; set; }
}
