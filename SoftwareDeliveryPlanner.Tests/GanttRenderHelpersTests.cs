using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Web.Services.Gantt;

namespace SoftwareDeliveryPlanner.Tests;

/// <summary>
/// Unit tests for the pure Gantt rendering helpers extracted from <c>Gantt.razor</c>.
/// Covers status precedence, hidden-predecessor count, effective-end calc, name truncation,
/// priority badges, and the critical-path algorithm (longest-path with cycle guard).
/// </summary>
public class GanttRenderHelpersTests
{
    private static readonly List<EffortBreakdownSpec> StdBreakdown = new()
    {
        new("DEV", 10, 0, 1.0),
        new("QA", 2, 0, 1.0),
    };

    /// <summary>
    /// Normalises a short test label (e.g. "T1", "A", "LONG") into a valid
    /// <c>AAA-NNN</c> Task ID by hashing the label deterministically. Predecessor
    /// references must be normalised the same way so the strings match.
    /// </summary>
    internal static string Norm(string label)
    {
        var clean = new string(label.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (clean.Length == 0) clean = "X";
        // Take 3 letters (pad with X) and 3 digits derived from a stable hash.
        var letters = new string(clean.Where(char.IsLetter).Concat("XXX").Take(3).ToArray());
        var hash = 0;
        foreach (var ch in clean) hash = (hash * 31 + ch) & 0x7FFFFFFF;
        var digits = (hash % 1000).ToString("D3");
        return $"{letters}-{digits}";
    }

    private static TaskItem MakeTask(
        string id,
        int duration = 10,
        int? priority = null,
        string? status = null,
        string? risk = null,
        string[]? predecessors = null)
    {
        var normId = Norm(id);
        var t = TaskItem.Create(normId, "Test " + id, priority ?? 5, StdBreakdown);
        // Apply scheduling result so PlannedStart/Finish/Duration/Status are set, with optional risk.
        var start = new DateTime(2026, 1, 1);
        t.ApplySchedulingResult(
            peakConcurrency: 1.0,
            plannedStart: start,
            plannedFinish: start.AddDays(duration),
            duration: duration,
            status: status ?? DomainConstants.TaskStatus.NotStarted,
            deliveryRisk: risk ?? DomainConstants.DeliveryRisk.OnTrack);

        if (predecessors is not null)
            foreach (var p in predecessors)
                t.AddDependency(Norm(p));

        return t;
    }

    // ── GetStatusClass — risk precedence ─────────────────────────────────────

    [Fact]
    public void GetStatusClass_LateRisk_OverridesInProgressStatus()
    {
        var task = MakeTask("T1", status: DomainConstants.TaskStatus.InProgress, risk: DomainConstants.DeliveryRisk.Late);
        Assert.Equal("gantt-status-late", GanttRenderHelpers.GetStatusClass(task));
    }

    [Fact]
    public void GetStatusClass_AtRisk_OverridesNotStartedStatus()
    {
        var task = MakeTask("T2", status: DomainConstants.TaskStatus.NotStarted, risk: DomainConstants.DeliveryRisk.AtRisk);
        Assert.Equal("gantt-status-atrisk", GanttRenderHelpers.GetStatusClass(task));
    }

    [Fact]
    public void GetStatusClass_CompletedStatus_TakesPrecedenceOverOnTrackRisk()
    {
        var task = MakeTask("T3", status: DomainConstants.TaskStatus.Completed, risk: DomainConstants.DeliveryRisk.OnTrack);
        Assert.Equal("gantt-status-completed", GanttRenderHelpers.GetStatusClass(task));
    }

    [Fact]
    public void GetStatusClass_InProgress_OnTrack_ReturnsInProgressClass()
    {
        var task = MakeTask("T4", status: DomainConstants.TaskStatus.InProgress, risk: DomainConstants.DeliveryRisk.OnTrack);
        Assert.Equal("gantt-status-inprogress", GanttRenderHelpers.GetStatusClass(task));
    }

    [Fact]
    public void GetStatusClass_NotStarted_OnTrack_ReturnsNotStartedClass()
    {
        var task = MakeTask("T5", status: DomainConstants.TaskStatus.NotStarted, risk: DomainConstants.DeliveryRisk.OnTrack);
        Assert.Equal("gantt-status-notstarted", GanttRenderHelpers.GetStatusClass(task));
    }

    [Fact]
    public void GetStatusClass_NullTask_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => GanttRenderHelpers.GetStatusClass(null!));
    }

    // ── CountHiddenPredecessors ──────────────────────────────────────────────

    [Fact]
    public void CountHiddenPredecessors_NoDependencies_ReturnsZero()
    {
        var task = MakeTask("T1");
        var visible = new HashSet<string> { Norm("T1") };
        Assert.Equal(0, GanttRenderHelpers.CountHiddenPredecessors(task, visible));
    }

    [Fact]
    public void CountHiddenPredecessors_AllPredecessorsVisible_ReturnsZero()
    {
        var task = MakeTask("T2", predecessors: new[] { "P1", "P2" });
        var visible = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Norm("T2"), Norm("P1"), Norm("P2") };
        Assert.Equal(0, GanttRenderHelpers.CountHiddenPredecessors(task, visible));
    }

    [Fact]
    public void CountHiddenPredecessors_OnePredecessorHidden_ReturnsOne()
    {
        var task = MakeTask("T3", predecessors: new[] { "P1", "P2" });
        var visible = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Norm("T3"), Norm("P1") };
        Assert.Equal(1, GanttRenderHelpers.CountHiddenPredecessors(task, visible));
    }

    [Fact]
    public void CountHiddenPredecessors_AllPredecessorsHidden_ReturnsAll()
    {
        var task = MakeTask("T4", predecessors: new[] { "P1", "P2", "P3" });
        var visible = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Norm("T4") };
        Assert.Equal(3, GanttRenderHelpers.CountHiddenPredecessors(task, visible));
    }

    // ── EffectiveEnd ─────────────────────────────────────────────────────────

    [Fact]
    public void EffectiveEnd_NoSegments_ReturnsPlannedFinish()
    {
        var task = MakeTask("T1", duration: 10);
        var end = GanttRenderHelpers.EffectiveEnd(task, Array.Empty<DateTime>());
        Assert.Equal(task.PlannedFinish, end);
    }

    [Fact]
    public void EffectiveEnd_SegmentsEndEarlier_ReturnsPlannedFinish()
    {
        var task = MakeTask("T2", duration: 10);
        var earlier = new[] { task.PlannedStart!.Value.AddDays(2), task.PlannedStart.Value.AddDays(5) };
        var end = GanttRenderHelpers.EffectiveEnd(task, earlier);
        Assert.Equal(task.PlannedFinish, end);
    }

    [Fact]
    public void EffectiveEnd_SegmentsEndLater_ExtendsBarPastPlannedFinish()
    {
        var task = MakeTask("T3", duration: 10);
        var later = new[] { task.PlannedFinish!.Value.AddDays(3) };
        var end = GanttRenderHelpers.EffectiveEnd(task, later);
        Assert.Equal(task.PlannedFinish.Value.AddDays(3), end);
    }

    // ── TruncateName ─────────────────────────────────────────────────────────

    [Fact]
    public void TruncateName_ShorterThanMax_ReturnsAsIs()
    {
        Assert.Equal("Hello", GanttRenderHelpers.TruncateName("Hello", 20));
    }

    [Fact]
    public void TruncateName_ExactlyMax_ReturnsAsIs()
    {
        var s = new string('x', 20);
        Assert.Equal(s, GanttRenderHelpers.TruncateName(s, 20));
    }

    [Fact]
    public void TruncateName_LongerThanMax_AppendsEllipsisAtMaxMinusOne()
    {
        var s = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"; // 26 chars
        var truncated = GanttRenderHelpers.TruncateName(s, 20);
        Assert.Equal(20, truncated.Length);
        Assert.EndsWith("\u2026", truncated);
        Assert.Equal("ABCDEFGHIJKLMNOPQRS\u2026", truncated);
    }

    [Fact]
    public void TruncateName_InvalidMaxLength_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GanttRenderHelpers.TruncateName("hi", 0));
    }

    // ── PriorityBadgeClass ───────────────────────────────────────────────────

    [Theory]
    [InlineData(1, "gantt-priority-high")]
    [InlineData(3, "gantt-priority-high")]
    [InlineData(4, "gantt-priority-medium")]
    [InlineData(6, "gantt-priority-medium")]
    [InlineData(7, "gantt-priority-low")]
    [InlineData(10, "gantt-priority-low")]
    public void PriorityBadgeClass_BucketsCorrectly(int priority, string expected)
    {
        Assert.Equal(expected, GanttRenderHelpers.PriorityBadgeClass(priority));
    }

    // ── ComputeCriticalPath ──────────────────────────────────────────────────

    [Fact]
    public void ComputeCriticalPath_EmptyTaskList_ReturnsEmpty()
    {
        var result = GanttRenderHelpers.ComputeCriticalPath(Array.Empty<TaskItem>());
        Assert.Empty(result);
    }

    [Fact]
    public void ComputeCriticalPath_SingleTask_ReturnsThatTask()
    {
        var t = MakeTask("ONLY", duration: 5);
        var result = GanttRenderHelpers.ComputeCriticalPath(new[] { t });
        Assert.Single(result);
        Assert.Contains(Norm("ONLY"), result);
    }

    [Fact]
    public void ComputeCriticalPath_NoDependencies_PicksLongestSingletonByDuration()
    {
        var t1 = MakeTask("SHORT", duration: 3);
        var t2 = MakeTask("LONG", duration: 10);
        var t3 = MakeTask("MEDIUM", duration: 5);
        var result = GanttRenderHelpers.ComputeCriticalPath(new[] { t1, t2, t3 });
        Assert.Single(result);
        Assert.Contains(Norm("LONG"), result);
    }

    [Fact]
    public void ComputeCriticalPath_LinearChain_HonoursTaskDuration()
    {
        // A(2) -> B(5) -> C(3), competing single-task D(7).
        // Chain duration = 2 + 5 + 3 = 10 > 7, so chain wins.
        var a = MakeTask("A", duration: 2);
        var b = MakeTask("B", duration: 5, predecessors: new[] { "A" });
        var c = MakeTask("C", duration: 3, predecessors: new[] { "B" });
        var d = MakeTask("D", duration: 7);

        var result = GanttRenderHelpers.ComputeCriticalPath(new[] { a, b, c, d });
        Assert.Equal(3, result.Count);
        Assert.Contains(Norm("A"), result);
        Assert.Contains(Norm("B"), result);
        Assert.Contains(Norm("C"), result);
        Assert.DoesNotContain(Norm("D"), result);
    }

    [Fact]
    public void ComputeCriticalPath_DiamondGraph_ChoosesLongerBranch()
    {
        // A(1) -> B(10) -> D(1)  (length 12)
        // A(1) -> C(2)  -> D(1)  (length 4)
        var a = MakeTask("A", duration: 1);
        var b = MakeTask("B", duration: 10, predecessors: new[] { "A" });
        var c = MakeTask("C", duration: 2, predecessors: new[] { "A" });
        var d = MakeTask("D", duration: 1, predecessors: new[] { "B", "C" });

        var result = GanttRenderHelpers.ComputeCriticalPath(new[] { a, b, c, d });
        Assert.Contains(Norm("A"), result);
        Assert.Contains(Norm("B"), result);
        Assert.Contains(Norm("D"), result);
        // C is not on the longest path
        Assert.DoesNotContain(Norm("C"), result);
    }

    [Fact]
    public void ComputeCriticalPath_HandlesCycle_NoStackOverflow()
    {
        // A -> B -> A (cycle). Algorithm should terminate via visiting-set guard.
        var a = MakeTask("A", duration: 5, predecessors: new[] { "B" });
        var b = MakeTask("B", duration: 5, predecessors: new[] { "A" });

        var ex = Record.Exception(() => GanttRenderHelpers.ComputeCriticalPath(new[] { a, b }));
        Assert.Null(ex);
    }
}
