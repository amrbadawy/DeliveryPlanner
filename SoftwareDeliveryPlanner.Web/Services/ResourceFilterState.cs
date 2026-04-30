namespace SoftwareDeliveryPlanner.Web.Services;

public sealed class ResourceFilterState
{
    private readonly HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _seniorities = new(StringComparer.OrdinalIgnoreCase);

    public event Action? OnChange;

    public string SearchTerm { get; private set; } = string.Empty;
    public IReadOnlySet<string> SelectedRoles => _roles;
    public IReadOnlySet<string> SelectedSeniorities => _seniorities;

    public bool IsAnyActive => !string.IsNullOrWhiteSpace(SearchTerm) || _roles.Count > 0 || _seniorities.Count > 0;

    public void SetSearchTerm(string value)
    {
        var next = value?.Trim() ?? string.Empty;
        if (string.Equals(SearchTerm, next, StringComparison.Ordinal))
            return;

        SearchTerm = next;
        OnChange?.Invoke();
    }

    public void ToggleRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return;

        if (!_roles.Add(role))
            _roles.Remove(role);

        OnChange?.Invoke();
    }

    public void ToggleSeniority(string seniority)
    {
        if (string.IsNullOrWhiteSpace(seniority))
            return;

        if (!_seniorities.Add(seniority))
            _seniorities.Remove(seniority);

        OnChange?.Invoke();
    }

    public void Clear()
    {
        SearchTerm = string.Empty;
        _roles.Clear();
        _seniorities.Clear();
        OnChange?.Invoke();
    }

    public bool Matches(ResourceFilterItem item)
    {
        var searchOk = string.IsNullOrWhiteSpace(SearchTerm)
            || item.ResourceId.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
            || item.ResourceName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase);

        if (!searchOk)
            return false;

        var roleOk = _roles.Count == 0 || _roles.Contains(item.Role);
        if (!roleOk)
            return false;

        var seniorityOk = _seniorities.Count == 0 || _seniorities.Contains(item.SeniorityLevel);
        return seniorityOk;
    }

    public sealed record ResourceFilterItem(string ResourceId, string ResourceName, string Role, string SeniorityLevel);
}
