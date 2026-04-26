namespace SoftwareDeliveryPlanner.Domain.Models;

public class TaskStatusLookup
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
