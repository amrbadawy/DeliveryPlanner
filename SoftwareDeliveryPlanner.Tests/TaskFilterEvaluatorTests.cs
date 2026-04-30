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
}
