using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace SoftwareDeliveryPlanner.Web.Services;

/// <summary>
/// Scoped per-circuit filter state shared across pages that opt-in via Bind(pageKey).
/// Each page maintains its own dimensional filter independently (keyed by pageKey),
/// but selections are exposed via the same API so the sidebar component can be reused.
///
/// Multi-select chip dimensions:
///   - Status         (TaskItem.Status — DomainConstants.TaskStatus values)
///   - Risk           (TaskItem.DeliveryRisk — DomainConstants.DeliveryRisk values)
///   - Priority       (TaskItem.Priority bucketed: 1-3 High, 4-6 Medium, 7-10 Low)
///   - Phase          (TaskItem.Phase — free-text)
///   - Role           (derived from EffortBreakdown.Role)
///   - DependencyState (NoDeps / HasDeps)
///
/// Plus a free-text Search term (composes with the page's existing search).
///
/// State is published via OnChange; pages subscribe and call StateHasChanged.
/// URL sync is bidirectional via NavigationManager — chips serialize as
/// comma-separated values per dimension (?status=IN_PROGRESS,COMPLETED&role=DEV,QA).
/// </summary>
public sealed class TaskFilterState : IDisposable
{
    public const string PageKeyTasks = "tasks";
    public const string PageKeyGantt = "gantt";

    private readonly NavigationManager _nav;
    private readonly Dictionary<string, PageFilters> _byPage = new(StringComparer.Ordinal);
    private string? _activePageKey;
    private bool _suppressUrlWrite;

    public TaskFilterState(NavigationManager nav)
    {
        _nav = nav;
        _nav.LocationChanged += OnLocationChanged;
    }

    public event Action? OnChange;

    /// <summary>Bind a page (Tasks or Gantt) to this state instance and load any URL params for it.</summary>
    public void Bind(string pageKey)
    {
        _activePageKey = pageKey;
        if (!_byPage.ContainsKey(pageKey))
            _byPage[pageKey] = new PageFilters();
        LoadFromUrl();
    }

    public PageFilters Current => _activePageKey != null && _byPage.TryGetValue(_activePageKey, out var p)
        ? p : new PageFilters();

    public string SearchTerm
    {
        get => Current.SearchTerm;
        set { if (Current.SearchTerm != value) { Current.SearchTerm = value; Notify(); } }
    }

    public IReadOnlySet<string> SelectedStatuses => Current.Statuses;
    public IReadOnlySet<string> SelectedRisks => Current.Risks;
    public IReadOnlySet<string> SelectedPriorityBuckets => Current.PriorityBuckets;
    public IReadOnlySet<string> SelectedPhases => Current.Phases;
    public IReadOnlySet<string> SelectedRoles => Current.Roles;
    public IReadOnlySet<string> SelectedDependencyStates => Current.DependencyStates;

    public void ToggleStatus(string code) => ToggleSet(Current.Statuses, code);
    public void ToggleRisk(string code) => ToggleSet(Current.Risks, code);
    public void TogglePriorityBucket(string bucket) => ToggleSet(Current.PriorityBuckets, bucket);
    public void TogglePhase(string phase) => ToggleSet(Current.Phases, phase);
    public void ToggleRole(string role) => ToggleSet(Current.Roles, role);
    public void ToggleDependencyState(string state) => ToggleSet(Current.DependencyStates, state);

    public bool IsAnyActive => !string.IsNullOrWhiteSpace(Current.SearchTerm)
        || Current.Statuses.Count > 0
        || Current.Risks.Count > 0
        || Current.PriorityBuckets.Count > 0
        || Current.Phases.Count > 0
        || Current.Roles.Count > 0
        || Current.DependencyStates.Count > 0;

    public int ActiveCount =>
        (string.IsNullOrWhiteSpace(Current.SearchTerm) ? 0 : 1)
        + Current.Statuses.Count
        + Current.Risks.Count
        + Current.PriorityBuckets.Count
        + Current.Phases.Count
        + Current.Roles.Count
        + Current.DependencyStates.Count;

    public void Clear()
    {
        if (_activePageKey == null) return;
        _byPage[_activePageKey] = new PageFilters();
        Notify();
    }

    private void ToggleSet(HashSet<string> set, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!set.Add(value)) set.Remove(value);
        Notify();
    }

    private void Notify()
    {
        WriteToUrl();
        OnChange?.Invoke();
    }

    // ── URL sync ────────────────────────────────────────────────────────────
    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (_activePageKey == null) return;
        // Re-read URL on external navigation (e.g. Home page deep links).
        LoadFromUrl();
        OnChange?.Invoke();
    }

    private void LoadFromUrl()
    {
        if (_activePageKey == null) return;
        var page = _byPage[_activePageKey];
        var uri = new Uri(_nav.Uri);
        var q = System.Web.HttpUtility.ParseQueryString(uri.Query);

        page.SearchTerm = q["q"] ?? string.Empty;
        ReplaceSet(page.Statuses, q["status"]);
        ReplaceSet(page.Risks, q["risk"]);
        ReplaceSet(page.PriorityBuckets, q["pri"]);
        ReplaceSet(page.Phases, q["phase"]);
        ReplaceSet(page.Roles, q["role"]);
        ReplaceSet(page.DependencyStates, q["dep"]);
    }

    private void WriteToUrl()
    {
        if (_suppressUrlWrite || _activePageKey == null) return;
        var page = _byPage[_activePageKey];

        var dict = new Dictionary<string, object?>
        {
            ["q"] = string.IsNullOrWhiteSpace(page.SearchTerm) ? null : page.SearchTerm,
            ["status"] = SerializeSet(page.Statuses),
            ["risk"] = SerializeSet(page.Risks),
            ["pri"] = SerializeSet(page.PriorityBuckets),
            ["phase"] = SerializeSet(page.Phases),
            ["role"] = SerializeSet(page.Roles),
            ["dep"] = SerializeSet(page.DependencyStates),
        };

        var newUri = _nav.GetUriWithQueryParameters(dict);
        _suppressUrlWrite = true;
        try { _nav.NavigateTo(newUri, forceLoad: false, replace: true); }
        finally { _suppressUrlWrite = false; }
    }

    private static string? SerializeSet(HashSet<string> set) =>
        set.Count == 0 ? null : string.Join(',', set.OrderBy(x => x, StringComparer.Ordinal));

    private static void ReplaceSet(HashSet<string> set, string? csv)
    {
        set.Clear();
        if (string.IsNullOrWhiteSpace(csv)) return;
        foreach (var item in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            set.Add(item);
    }

    public void Dispose() => _nav.LocationChanged -= OnLocationChanged;

    public sealed class PageFilters
    {
        public string SearchTerm { get; set; } = string.Empty;
        public HashSet<string> Statuses { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Risks { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> PriorityBuckets { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Phases { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Roles { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> DependencyStates { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public static class PriorityBuckets
    {
        public const string High = "HIGH";    // 1–3
        public const string Medium = "MEDIUM"; // 4–6
        public const string Low = "LOW";      // 7–10

        public static string FromPriority(int priority) =>
            priority <= 3 ? High : priority <= 6 ? Medium : Low;
    }

    public static class DependencyStates
    {
        public const string HasDependencies = "HAS_DEPS";
        public const string NoDependencies = "NO_DEPS";
    }
}
