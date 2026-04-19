namespace SoftwareDeliveryPlanner.Domain.Models;

public class Role
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 1;
}
