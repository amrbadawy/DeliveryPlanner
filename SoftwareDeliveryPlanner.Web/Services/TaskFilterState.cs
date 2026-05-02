using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using System.Text.Json;
using System.Text.Json.Serialization;

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
/// Plus a free-text Search term (composes with the page's existing search) and
/// per-task Pin/Hide sets (Pinned tasks float to top; Hidden tasks are removed
/// from the page's rendered list — Gantt renders ghost-arrow stubs for hidden
/// predecessors of visible tasks).
///
/// State is published via OnChange; pages subscribe and call StateHasChanged.
/// URL sync is bidirectional via NavigationManager — chips serialize as
/// comma-separated values per dimension (?status=IN_PROGRESS,COMPLETED&role=DEV,QA).
/// Pin/Hide sets are not URL-synced (too noisy / per-row scope) but are part
/// of saved-view payloads.
/// </summary>
public sealed class TaskFilterState : IDisposable
{
    public const string PageKeyTasks = "tasks";
    public const string PageKeyGantt = "gantt";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    private readonly NavigationManager _nav;
    private readonly AuthenticationStateProvider? _authStateProvider;
    private readonly Dictionary<string, PageFilters> _byPage = new(StringComparer.Ordinal);
    private string? _activePageKey;
    private bool _suppressUrlWrite;
    private string? _cachedOwnerKey;

    public TaskFilterState(NavigationManager nav, AuthenticationStateProvider? authStateProvider = null)
    {
        _nav = nav;
        _authStateProvider = authStateProvider;
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

    public string ActivePageKey => _activePageKey ?? string.Empty;

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
    public IReadOnlySet<string> PinnedTaskIds => Current.PinnedTaskIds;
    public IReadOnlySet<string> HiddenTaskIds => Current.HiddenTaskIds;

    public void ToggleStatus(string code) => ToggleSet(Current.Statuses, code);
    public void ToggleRisk(string code) => ToggleSet(Current.Risks, code);
    public void TogglePriorityBucket(string bucket) => ToggleSet(Current.PriorityBuckets, bucket);
    public void TogglePhase(string phase) => ToggleSet(Current.Phases, phase);
    public void ToggleRole(string role) => ToggleSet(Current.Roles, role);
    public void ToggleDependencyState(string state) => ToggleSet(Current.DependencyStates, state);

    /// <summary>Selects all values in a chip-based dimension from the given universe.</summary>
    public void SelectAllDimension(string dimension, IEnumerable<string> universe)
    {
        var set = GetDimensionSet(dimension);
        if (set == null) return;
        foreach (var item in universe)
            if (!string.IsNullOrWhiteSpace(item)) set.Add(item);
        Notify();
    }

    /// <summary>Clears all selections in a chip-based dimension.</summary>
    public void ClearDimension(string dimension)
    {
        var set = GetDimensionSet(dimension);
        if (set == null || set.Count == 0) return;
        set.Clear();
        Notify();
    }

    /// <summary>Inverts the selection in a chip-based dimension relative to the given universe.</summary>
    public void InvertDimension(string dimension, IEnumerable<string> universe)
    {
        var set = GetDimensionSet(dimension);
        if (set == null) return;
        foreach (var item in universe)
        {
            if (string.IsNullOrWhiteSpace(item)) continue;
            if (!set.Remove(item)) set.Add(item);
        }
        Notify();
    }

    private HashSet<string>? GetDimensionSet(string dimension) => dimension switch
    {
        "status"   => Current.Statuses,
        "risk"     => Current.Risks,
        "priority" => Current.PriorityBuckets,
        "phase"    => Current.Phases,
        "role"     => Current.Roles,
        "dep"      => Current.DependencyStates,
        _          => null
    };

    /// <summary>Pin a task to the top of the rendered list. Pinning auto-unhides.</summary>
    public void TogglePin(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId)) return;
        if (!Current.PinnedTaskIds.Add(taskId)) Current.PinnedTaskIds.Remove(taskId);
        else Current.HiddenTaskIds.Remove(taskId);
        // Pin/hide are not URL-synced; just notify.
        OnChange?.Invoke();
    }

    /// <summary>Hide a task from the rendered list. Hiding auto-unpins.</summary>
    public void ToggleHide(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId)) return;
        if (!Current.HiddenTaskIds.Add(taskId)) Current.HiddenTaskIds.Remove(taskId);
        else Current.PinnedTaskIds.Remove(taskId);
        OnChange?.Invoke();
    }

    public bool IsPinned(string taskId) => Current.PinnedTaskIds.Contains(taskId);
    public bool IsHidden(string taskId) => Current.HiddenTaskIds.Contains(taskId);

    public void PinMany(IEnumerable<string> taskIds)
    {
        var changed = false;
        foreach (var id in taskIds)
        {
            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (Current.HiddenTaskIds.Remove(id))
                changed = true;
            if (Current.PinnedTaskIds.Add(id))
                changed = true;
        }

        if (changed)
            OnChange?.Invoke();
    }

    public void HideMany(IEnumerable<string> taskIds)
    {
        var changed = false;
        foreach (var id in taskIds)
        {
            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (Current.PinnedTaskIds.Remove(id))
                changed = true;
            if (Current.HiddenTaskIds.Add(id))
                changed = true;
        }

        if (changed)
            OnChange?.Invoke();
    }

    public bool IsAnyActive => !string.IsNullOrWhiteSpace(Current.SearchTerm)
        || Current.Statuses.Count > 0
        || Current.Risks.Count > 0
        || Current.PriorityBuckets.Count > 0
        || Current.Phases.Count > 0
        || Current.Roles.Count > 0
        || Current.DependencyStates.Count > 0
        || Current.PinnedTaskIds.Count > 0
        || Current.HiddenTaskIds.Count > 0;

    public int ActiveCount =>
        (string.IsNullOrWhiteSpace(Current.SearchTerm) ? 0 : 1)
        + Current.Statuses.Count
        + Current.Risks.Count
        + Current.PriorityBuckets.Count
        + Current.Phases.Count
        + Current.Roles.Count
        + Current.DependencyStates.Count
        + Current.PinnedTaskIds.Count
        + Current.HiddenTaskIds.Count;

    public void Clear()
    {
        if (_activePageKey == null) return;
        _byPage[_activePageKey] = new PageFilters();
        Notify();
    }

    // ── Saved-view payload (JSON) ───────────────────────────────────────────
    /// <summary>Serialize the current page's filter selections (incl. pin/hide) for storage.</summary>
    public string SerializeCurrentAsPayload()
    {
        var c = Current;
        var dto = new SavedViewPayload
        {
            SearchTerm = string.IsNullOrWhiteSpace(c.SearchTerm) ? null : c.SearchTerm,
            Statuses = c.Statuses.Count == 0 ? null : c.Statuses.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            Risks = c.Risks.Count == 0 ? null : c.Risks.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            PriorityBuckets = c.PriorityBuckets.Count == 0 ? null : c.PriorityBuckets.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            Phases = c.Phases.Count == 0 ? null : c.Phases.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            Roles = c.Roles.Count == 0 ? null : c.Roles.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            DependencyStates = c.DependencyStates.Count == 0 ? null : c.DependencyStates.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            PinnedTaskIds = c.PinnedTaskIds.Count == 0 ? null : c.PinnedTaskIds.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            HiddenTaskIds = c.HiddenTaskIds.Count == 0 ? null : c.HiddenTaskIds.OrderBy(x => x, StringComparer.Ordinal).ToList(),
        };
        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    /// <summary>Apply a saved-view payload to the current page, replacing all selections.</summary>
    public void ApplyPayload(string payloadJson)
    {
        if (_activePageKey == null) return;
        SavedViewPayload? dto;
        try { dto = JsonSerializer.Deserialize<SavedViewPayload>(payloadJson, JsonOptions); }
        catch (JsonException) { return; }
        if (dto is null) return;

        var fresh = new PageFilters
        {
            SearchTerm = dto.SearchTerm ?? string.Empty,
        };
        AddAll(fresh.Statuses, dto.Statuses);
        AddAll(fresh.Risks, dto.Risks);
        AddAll(fresh.PriorityBuckets, dto.PriorityBuckets);
        AddAll(fresh.Phases, dto.Phases);
        AddAll(fresh.Roles, dto.Roles);
        AddAll(fresh.DependencyStates, dto.DependencyStates);
        AddAll(fresh.PinnedTaskIds, dto.PinnedTaskIds);
        AddAll(fresh.HiddenTaskIds, dto.HiddenTaskIds);
        _byPage[_activePageKey] = fresh;
        Notify();
    }

    // ── Owner key (auth-aware, with caching) ────────────────────────────────
    /// <summary>
    /// Resolves the owner key for saved-view scoping. Returns the authenticated
    /// user's name when an AuthenticationStateProvider is registered AND the
    /// user is authenticated; otherwise null (= global/shared scope).
    /// Cached per-circuit so we don't roundtrip on every save/list.
    /// </summary>
    public async Task<string?> GetOwnerKeyAsync()
    {
        if (_cachedOwnerKey is not null) return _cachedOwnerKey == "\0" ? null : _cachedOwnerKey;
        if (_authStateProvider is null) { _cachedOwnerKey = "\0"; return null; }
        try
        {
            var state = await _authStateProvider.GetAuthenticationStateAsync();
            var name = state.User.Identity?.IsAuthenticated == true ? state.User.Identity.Name : null;
            _cachedOwnerKey = string.IsNullOrWhiteSpace(name) ? "\0" : name;
            return _cachedOwnerKey == "\0" ? null : _cachedOwnerKey;
        }
        catch
        {
            _cachedOwnerKey = "\0";
            return null;
        }
    }

    private void ToggleSet(HashSet<string> set, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!set.Add(value)) set.Remove(value);
        Notify();
    }

    private static void AddAll(HashSet<string> set, IEnumerable<string>? items)
    {
        if (items is null) return;
        foreach (var item in items)
            if (!string.IsNullOrWhiteSpace(item)) set.Add(item);
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
            ["view"] = null,
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
        public HashSet<string> PinnedTaskIds { get; } = new(StringComparer.Ordinal);
        public HashSet<string> HiddenTaskIds { get; } = new(StringComparer.Ordinal);
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

    /// <summary>JSON DTO persisted in SavedView.PayloadJson. Versionless; additive only.</summary>
    public sealed class SavedViewPayload
    {
        public string? SearchTerm { get; set; }
        public List<string>? Statuses { get; set; }
        public List<string>? Risks { get; set; }
        public List<string>? PriorityBuckets { get; set; }
        public List<string>? Phases { get; set; }
        public List<string>? Roles { get; set; }
        public List<string>? DependencyStates { get; set; }
        public List<string>? PinnedTaskIds { get; set; }
        public List<string>? HiddenTaskIds { get; set; }
    }
}
