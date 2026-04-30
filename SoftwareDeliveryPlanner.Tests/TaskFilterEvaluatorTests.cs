using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Tests.Infrastructure;
using SoftwareDeliveryPlanner.Web.Services;

namespace SoftwareDeliveryPlanner.Tests;

/// <summary>
/// Unit tests for the sidebar filter predicate (TaskFilterEvaluator) and the
/// PriorityBuckets helper. State (TaskFilterState) is integration-tested via
/// the e2e suite — its NavigationManager dependency makes it impractical
/// to unit-test in isolation without ASP.NET Core hosting infrastructure.
/// </summary>
public class TaskFilterEvaluatorTests
{
    private static List<EffortBreakdownSpec> B(double dev, double qa = 1) =>
        TestDatabaseHelper.MakeBreakdown(dev, qa);

    private static TaskItem MakeTask(
        string id = "SVC-001",
        string name = "Service",
        int priority = 5,
        string? phase = null,
        params string[] roles)
    {
        // Default to DEV+QA which are the required roles
        var breakdown = roles.Length == 0
            ? B(2)
            : roles.Select(r => new EffortBreakdownSpec(r, 1, 0, 1, null)).ToList();

        // Ensure DEV/QA always present (domain invariant)
        if (!breakdown.Any(b => b.Role == "DEV"))
            breakdown.Add(new EffortBreakdownSpec("DEV", 1, 0, 1, null));
        if (!breakdown.Any(b => b.Role == "QA"))
            breakdown.Add(new EffortBreakdownSpec("QA", 1, 0, 1, null));

        return TaskItem.Create(id, name, priority, breakdown, phase: phase);
    }

    private static TaskFilterState.PageFilters Filters() => new();

    // ── Empty filter ────────────────────────────────────────────────────────

    [Fact]
    public void EmptyFilter_MatchesAnyTask()
    {
        var task = MakeTask();
        Assert.True(TaskFilterEvaluator.Matches(task, Filters()));
    }

    // ── Search ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("SVC-001", true)]   // match by ID exact
    [InlineData("svc", true)]        // match by ID substring case-insensitive
    [InlineData("Payment", true)]    // match by name
    [InlineData("PAY", true)]        // match by name case-insensitive
    [InlineData("xyz", false)]       // no match
    public void Search_MatchesIdOrServiceName(string term, bool expected)
    {
        var task = MakeTask("SVC-001", "Payment Gateway");
        var f = Filters();
        f.SearchTerm = term;
        Assert.Equal(expected, TaskFilterEvaluator.Matches(task, f));
    }

    [Fact]
    public void Search_AlsoMatchesPhase()
    {
        var task = MakeTask("SVC-002", "Service", phase: "Discovery");
        var f = Filters();
        f.SearchTerm = "discov";
        Assert.True(TaskFilterEvaluator.Matches(task, f));
    }

    [Fact]
    public void Search_WhitespaceOnly_TreatedAsEmpty()
    {
        var task = MakeTask();
        var f = Filters();
        f.SearchTerm = "   ";
        Assert.True(TaskFilterEvaluator.Matches(task, f));
    }

    // ── Status / Risk ───────────────────────────────────────────────────────

    [Fact]
    public void Status_SingleSelection_ExcludesOthers()
    {
        var task = MakeTask(); // default status = NOT_STARTED
        var f = Filters();
        f.Statuses.Add(DomainConstants.TaskStatus.Completed);
        Assert.False(TaskFilterEvaluator.Matches(task, f));

        f.Statuses.Clear();
        f.Statuses.Add(DomainConstants.TaskStatus.NotStarted);
        Assert.True(TaskFilterEvaluator.Matches(task, f));
    }

    [Fact]
    public void Status_MultipleSelections_OrWithinDimension()
    {
        var task = MakeTask();
        var f = Filters();
        f.Statuses.Add(DomainConstants.TaskStatus.Completed);
        f.Statuses.Add(DomainConstants.TaskStatus.NotStarted);
        Assert.True(TaskFilterEvaluator.Matches(task, f));
    }

    [Fact]
    public void Risk_DefaultOnTrack_MatchesOnTrackChip()
    {
        var task = MakeTask(); // default risk = ON_TRACK
        var f = Filters();
        f.Risks.Add(DomainConstants.DeliveryRisk.OnTrack);
        Assert.True(TaskFilterEvaluator.Matches(task, f));
    }

    // ── Priority buckets ────────────────────────────────────────────────────

    [Theory]
    [InlineData(1, "HIGH")]
    [InlineData(3, "HIGH")]
    [InlineData(4, "MEDIUM")]
    [InlineData(6, "MEDIUM")]
    [InlineData(7, "LOW")]
    [InlineData(10, "LOW")]
    public void PriorityBuckets_MapCorrectly(int priority, string expected)
    {
        Assert.Equal(expected, TaskFilterState.PriorityBuckets.FromPriority(priority));
    }

    [Fact]
    public void Priority_HighChip_MatchesPriority1()
    {
        var task = MakeTask(priority: 1);
        var f = Filters();
        f.PriorityBuckets.Add(TaskFilterState.PriorityBuckets.High);
        Assert.True(TaskFilterEvaluator.Matches(task, f));
    }

    [Fact]
    public void Priority_HighChip_DoesNotMatchPriority8()
    {
        var task = MakeTask(priority: 8);
        var f = Filters();
        f.PriorityBuckets.Add(TaskFilterState.PriorityBuckets.High);
        Assert.False(TaskFilterEvaluator.Matches(task, f));
    }

    // ── Phase ───────────────────────────────────────────────────────────────

    [Fact]
    public void Phase_Match()
    {
        var task = MakeTask(phase: "Build");
        var f = Filters();
        f.Phases.Add("Build");
        Assert.True(TaskFilterEvaluator.Matches(task, f));
    }

    [Fact]
    public void Phase_NoMatch()
    {
        var task = MakeTask(phase: "Build");
        var f = Filters();
        f.Phases.Add("Test");
        Assert.False(TaskFilterEvaluator.Matches(task, f));
    }

    [Fact]
    public void Phase_NullTaskPhase_MatchesEmptyChip()
    {
        var task = MakeTask(phase: null);
        var f = Filters();
        f.Phases.Add(string.Empty);
        Assert.True(TaskFilterEvaluator.Matches(task, f));
    }

    // ── Role (effort breakdown) ─────────────────────────────────────────────

    [Fact]
    public void Role_TaskWithDevQa_MatchesDevChip()
    {
        var task = MakeTask();
        var f = Filters();
        f.Roles.Add("DEV");
        Assert.True(TaskFilterEvaluator.Matches(task, f));
    }

    [Fact]
    public void Role_TaskWithoutBA_DoesNotMatchBaChip()
    {
        var task = MakeTask(); // only DEV+QA
        var f = Filters();
        f.Roles.Add("BA");
        Assert.False(TaskFilterEvaluator.Matches(task, f));
    }

    [Fact]
    public void Role_MultipleSelections_OrWithinDimension()
    {
        var task = MakeTask();
        var f = Filters();
        f.Roles.Add("BA"); // not present on task
        f.Roles.Add("DEV"); // present
        Assert.True(TaskFilterEvaluator.Matches(task, f));
    }

    // ── Dependency state ────────────────────────────────────────────────────

    [Fact]
    public void DependencyState_Independent_MatchesTaskWithNoDeps()
    {
        var task = MakeTask();
        var f = Filters();
        f.DependencyStates.Add(TaskFilterState.DependencyStates.NoDependencies);
        Assert.True(TaskFilterEvaluator.Matches(task, f));
    }

    [Fact]
    public void DependencyState_HasDeps_DoesNotMatchTaskWithNoDeps()
    {
        var task = MakeTask();
        var f = Filters();
        f.DependencyStates.Add(TaskFilterState.DependencyStates.HasDependencies);
        Assert.False(TaskFilterEvaluator.Matches(task, f));
    }

    [Fact]
    public void DependencyState_HasDeps_MatchesTaskWithDeps()
    {
        var pred = MakeTask("SVC-100", "Predecessor");
        var task = MakeTask("SVC-200", "Dependent");
        task.AddDependency(pred.TaskId);

        var f = Filters();
        f.DependencyStates.Add(TaskFilterState.DependencyStates.HasDependencies);
        Assert.True(TaskFilterEvaluator.Matches(task, f));
    }

    // ── Cross-dimension AND semantics ───────────────────────────────────────

    [Fact]
    public void MultipleDimensions_AndAcrossDimensions()
    {
        var task = MakeTask(priority: 2);
        var f = Filters();
        f.PriorityBuckets.Add(TaskFilterState.PriorityBuckets.High);
        f.Statuses.Add(DomainConstants.TaskStatus.NotStarted);
        Assert.True(TaskFilterEvaluator.Matches(task, f));

        // Add a chip that excludes
        f.Risks.Add(DomainConstants.DeliveryRisk.Late);
        Assert.False(TaskFilterEvaluator.Matches(task, f));
    }

    [Fact]
    public void SearchPlusChip_BothMustMatch()
    {
        var task = MakeTask("SVC-001", "Payment");
        var f = Filters();
        f.SearchTerm = "Payment";
        f.Statuses.Add(DomainConstants.TaskStatus.Completed); // task default = NOT_STARTED
        Assert.False(TaskFilterEvaluator.Matches(task, f));
    }

    // ── Pin / Hide (overrides) ──────────────────────────────────────────────

    [Fact]
    public void HiddenTask_IsAlwaysExcluded_RegardlessOfChips()
    {
        var task = MakeTask("SVC-009", "Reporting");
        var f = Filters();
        f.HiddenTaskIds.Add("SVC-009");
        Assert.False(TaskFilterEvaluator.Matches(task, f));

        // Even with no other filters, hidden trumps.
        Assert.False(TaskFilterEvaluator.Matches(task, f));
    }

    [Fact]
    public void HiddenTask_DoesNotAffectOtherTasks()
    {
        var hidden = MakeTask("SVC-001", "A");
        var visible = MakeTask("SVC-002", "B");
        var f = Filters();
        f.HiddenTaskIds.Add("SVC-001");
        Assert.False(TaskFilterEvaluator.Matches(hidden, f));
        Assert.True(TaskFilterEvaluator.Matches(visible, f));
    }

    [Fact]
    public void PinnedTask_BypassesAllChipFilters()
    {
        // Task is high-priority; chips select Low only — but pin makes it visible.
        var task = MakeTask("SVC-100", priority: 1);
        var f = Filters();
        f.PriorityBuckets.Add(TaskFilterState.PriorityBuckets.Low);
        Assert.False(TaskFilterEvaluator.Matches(task, f)); // baseline: chip excludes

        f.PinnedTaskIds.Add("SVC-100");
        Assert.True(TaskFilterEvaluator.Matches(task, f)); // pin overrides
    }

    [Fact]
    public void HiddenTrumpsPinned_WhenBothSet()
    {
        // Defensive: TogglePin/Hide on the state mutually-exclude, but a
        // tampered payload could theoretically contain both. Hide wins.
        var task = MakeTask("SVC-200");
        var f = Filters();
        f.PinnedTaskIds.Add("SVC-200");
        f.HiddenTaskIds.Add("SVC-200");
        Assert.False(TaskFilterEvaluator.Matches(task, f));
    }

    // ── SavedView payload roundtrip (state-level) ───────────────────────────

    [Fact]
    public void SavedViewPayload_RoundtripsAllDimensions()
    {
        var nav = new TestNavigationManager("http://localhost/tasks");
        var state = new TaskFilterState(nav);
        state.Bind(TaskFilterState.PageKeyTasks);

        state.SearchTerm = "payment";
        state.ToggleStatus(DomainConstants.TaskStatus.InProgress);
        state.ToggleRisk(DomainConstants.DeliveryRisk.Late);
        state.TogglePriorityBucket(TaskFilterState.PriorityBuckets.High);
        state.TogglePhase("Build");
        state.ToggleRole("DEV");
        state.ToggleDependencyState(TaskFilterState.DependencyStates.HasDependencies);
        state.TogglePin("SVC-001");
        state.ToggleHide("SVC-002");

        var payload = state.SerializeCurrentAsPayload();
        Assert.Contains("\"searchTerm\":\"payment\"", payload);
        Assert.Contains("\"pinnedTaskIds\":[\"SVC-001\"]", payload);
        Assert.Contains("\"hiddenTaskIds\":[\"SVC-002\"]", payload);

        // Apply onto a fresh state
        var nav2 = new TestNavigationManager("http://localhost/tasks");
        var fresh = new TaskFilterState(nav2);
        fresh.Bind(TaskFilterState.PageKeyTasks);
        fresh.ApplyPayload(payload);

        Assert.Equal("payment", fresh.SearchTerm);
        Assert.Contains(DomainConstants.TaskStatus.InProgress, fresh.SelectedStatuses);
        Assert.Contains(DomainConstants.DeliveryRisk.Late, fresh.SelectedRisks);
        Assert.Contains(TaskFilterState.PriorityBuckets.High, fresh.SelectedPriorityBuckets);
        Assert.Contains("Build", fresh.SelectedPhases);
        Assert.Contains("DEV", fresh.SelectedRoles);
        Assert.Contains(TaskFilterState.DependencyStates.HasDependencies, fresh.SelectedDependencyStates);
        Assert.Contains("SVC-001", fresh.PinnedTaskIds);
        Assert.Contains("SVC-002", fresh.HiddenTaskIds);
    }

    [Fact]
    public void ApplyPayload_OnEmptyJsonObject_ClearsCurrentState()
    {
        var nav = new TestNavigationManager("http://localhost/tasks");
        var state = new TaskFilterState(nav);
        state.Bind(TaskFilterState.PageKeyTasks);
        state.ToggleStatus(DomainConstants.TaskStatus.NotStarted);
        Assert.Single(state.SelectedStatuses);

        state.ApplyPayload("{}");
        Assert.Empty(state.SelectedStatuses);
        Assert.Equal(string.Empty, state.SearchTerm);
    }

    [Fact]
    public void ApplyPayload_OnInvalidJson_IsNoOp()
    {
        var nav = new TestNavigationManager("http://localhost/tasks");
        var state = new TaskFilterState(nav);
        state.Bind(TaskFilterState.PageKeyTasks);
        state.ToggleStatus(DomainConstants.TaskStatus.NotStarted);

        state.ApplyPayload("not-json-{{{");
        Assert.Single(state.SelectedStatuses); // unchanged
    }

    [Fact]
    public void TogglePin_UnhidesIfHidden()
    {
        var nav = new TestNavigationManager("http://localhost/tasks");
        var state = new TaskFilterState(nav);
        state.Bind(TaskFilterState.PageKeyTasks);

        state.ToggleHide("SVC-1");
        Assert.True(state.IsHidden("SVC-1"));

        state.TogglePin("SVC-1");
        Assert.True(state.IsPinned("SVC-1"));
        Assert.False(state.IsHidden("SVC-1"));
    }

    [Fact]
    public void ToggleHide_UnpinsIfPinned()
    {
        var nav = new TestNavigationManager("http://localhost/tasks");
        var state = new TaskFilterState(nav);
        state.Bind(TaskFilterState.PageKeyTasks);

        state.TogglePin("SVC-2");
        Assert.True(state.IsPinned("SVC-2"));

        state.ToggleHide("SVC-2");
        Assert.True(state.IsHidden("SVC-2"));
        Assert.False(state.IsPinned("SVC-2"));
    }
}

/// <summary>Test double for NavigationManager — captures NavigateTo calls without HTTP.</summary>
internal sealed class TestNavigationManager : Microsoft.AspNetCore.Components.NavigationManager
{
    public TestNavigationManager(string uri) => Initialize("http://localhost/", uri);

    protected override void NavigateToCore(string uri, bool forceLoad)
        => NavigateInternal(uri);

    protected override void NavigateToCore(string uri, Microsoft.AspNetCore.Components.NavigationOptions options)
        => NavigateInternal(uri);

    private void NavigateInternal(string uri)
    {
        var absolute = uri.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? uri
            : ToAbsoluteUri(uri).ToString();
        Uri = absolute;
        NotifyLocationChanged(isInterceptedLink: false);
    }
}
