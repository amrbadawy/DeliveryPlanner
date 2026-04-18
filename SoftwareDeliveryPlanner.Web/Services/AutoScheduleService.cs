namespace SoftwareDeliveryPlanner.Web.Services;

public class AutoScheduleService
{
    public bool IsEnabled { get; set; } = false;
    public event Action? OnChange;

    public void Toggle()
    {
        IsEnabled = !IsEnabled;
        OnChange?.Invoke();
    }
}
