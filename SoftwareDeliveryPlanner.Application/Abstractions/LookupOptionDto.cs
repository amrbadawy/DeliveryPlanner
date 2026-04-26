namespace SoftwareDeliveryPlanner.Application.Abstractions;

public sealed record LookupOptionDto(string Code, string DisplayName, int SortOrder, bool IsActive);

public static class LookupCatalogs
{
    public const string TaskStatuses = "task_statuses";
    public const string DeliveryRisks = "delivery_risks";
    public const string HolidayTypes = "holiday_types";
    public const string AdjustmentTypes = "adjustment_types";
    public const string ActiveStatuses = "active_statuses";
    public const string WorkingWeeks = "working_weeks";
}
