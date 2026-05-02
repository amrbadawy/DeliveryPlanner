using SoftwareDeliveryPlanner.Web.Services.Gantt;

namespace SoftwareDeliveryPlanner.Tests;

/// <summary>
/// Unit tests for <see cref="TimelineGeometry"/> — pure date↔percent and date↔pixel math
/// for the Gantt timeline. Validates plan-range buffer rules, today-line range checks,
/// bar geometry %, and the timeline pixel-width floor.
/// </summary>
public class TimelineGeometryTests
{
    // ── IsTodayInPlanRange ───────────────────────────────────────────────────

    [Fact]
    public void IsTodayInPlanRange_TodayBeforeStart_ReturnsFalse()
    {
        var today = new DateTime(2026, 1, 1);
        var start = new DateTime(2026, 2, 1);
        var end = new DateTime(2026, 3, 1);
        Assert.False(TimelineGeometry.IsTodayInPlanRange(today, start, end));
    }

    [Fact]
    public void IsTodayInPlanRange_TodayAfterEnd_ReturnsFalse()
    {
        var today = new DateTime(2026, 6, 1);
        var start = new DateTime(2026, 2, 1);
        var end = new DateTime(2026, 3, 1);
        Assert.False(TimelineGeometry.IsTodayInPlanRange(today, start, end));
    }

    [Fact]
    public void IsTodayInPlanRange_TodayWithinRange_ReturnsTrue()
    {
        var today = new DateTime(2026, 2, 15);
        var start = new DateTime(2026, 2, 1);
        var end = new DateTime(2026, 3, 1);
        Assert.True(TimelineGeometry.IsTodayInPlanRange(today, start, end));
    }

    [Fact]
    public void IsTodayInPlanRange_TodayEqualsStart_ReturnsTrue()
    {
        var d = new DateTime(2026, 2, 1);
        Assert.True(TimelineGeometry.IsTodayInPlanRange(d, d, d.AddDays(10)));
    }

    [Fact]
    public void IsTodayInPlanRange_TodayEqualsEnd_ReturnsTrue()
    {
        var d = new DateTime(2026, 2, 10);
        Assert.True(TimelineGeometry.IsTodayInPlanRange(d, d.AddDays(-10), d));
    }

    // ── DatePercent ──────────────────────────────────────────────────────────

    [Fact]
    public void DatePercent_StartOfPlan_ReturnsZero()
    {
        var start = new DateTime(2026, 1, 1);
        Assert.Equal(0, TimelineGeometry.DatePercent(start, start, totalDays: 10));
    }

    [Fact]
    public void DatePercent_EndOfPlan_ReturnsHundred()
    {
        var start = new DateTime(2026, 1, 1);
        var end = start.AddDays(10);
        Assert.Equal(100, TimelineGeometry.DatePercent(end, start, totalDays: 10));
    }

    [Fact]
    public void DatePercent_Midpoint_ReturnsFifty()
    {
        var start = new DateTime(2026, 1, 1);
        var mid = start.AddDays(5);
        Assert.Equal(50, TimelineGeometry.DatePercent(mid, start, totalDays: 10));
    }

    [Fact]
    public void DatePercent_TotalDaysZero_ReturnsZero()
    {
        Assert.Equal(0, TimelineGeometry.DatePercent(new DateTime(2026, 1, 5), new DateTime(2026, 1, 1), totalDays: 0));
    }

    // ── BarGeometry ──────────────────────────────────────────────────────────

    [Fact]
    public void BarGeometry_StandardSpan_ProducesExpectedLeftAndWidth()
    {
        var planStart = new DateTime(2026, 1, 1);
        var taskStart = planStart.AddDays(2);
        var taskEnd = planStart.AddDays(7);
        var (leftPct, widthPct) = TimelineGeometry.BarGeometry(taskStart, taskEnd, planStart, totalDays: 10);
        Assert.Equal(20, leftPct);
        Assert.Equal(50, widthPct);
    }

    [Fact]
    public void BarGeometry_ZeroDurationTask_ReturnsMinimumWidth()
    {
        var planStart = new DateTime(2026, 1, 1);
        var d = planStart.AddDays(3);
        var (_, widthPct) = TimelineGeometry.BarGeometry(d, d, planStart, totalDays: 10);
        Assert.Equal(0.5, widthPct);
    }

    // ── ApplyPlanRangeBuffer ─────────────────────────────────────────────────

    [Fact]
    public void ApplyPlanRangeBuffer_NoStrictDate_AddsMinusTwoPlusFiveDays()
    {
        var rawStart = new DateTime(2026, 2, 10);
        var rawEnd = new DateTime(2026, 2, 20);
        var (start, end, days) = TimelineGeometry.ApplyPlanRangeBuffer(rawStart, rawEnd);
        Assert.Equal(rawStart.AddDays(-2), start);
        Assert.Equal(rawEnd.AddDays(5), end);
        Assert.Equal(17, days); // 10 raw + 2 + 5
    }

    [Fact]
    public void ApplyPlanRangeBuffer_StrictDateLater_ExtendsPlanEndToStrictPlusFive()
    {
        var rawStart = new DateTime(2026, 2, 10);
        var rawEnd = new DateTime(2026, 2, 20);
        var strict = new DateTime(2026, 3, 1);
        var (start, end, _) = TimelineGeometry.ApplyPlanRangeBuffer(rawStart, rawEnd, strict);
        Assert.Equal(rawStart.AddDays(-2), start);
        Assert.Equal(strict.AddDays(5), end);
    }

    [Fact]
    public void ApplyPlanRangeBuffer_StrictDateEarlier_DoesNotShrink()
    {
        var rawStart = new DateTime(2026, 2, 10);
        var rawEnd = new DateTime(2026, 2, 20);
        var strict = new DateTime(2026, 2, 15); // earlier than rawEnd
        var (_, end, _) = TimelineGeometry.ApplyPlanRangeBuffer(rawStart, rawEnd, strict);
        Assert.Equal(rawEnd.AddDays(5), end);
    }

    [Fact]
    public void ApplyPlanRangeBuffer_TotalDaysNeverBelowOne()
    {
        var d = new DateTime(2026, 2, 10);
        var (_, _, days) = TimelineGeometry.ApplyPlanRangeBuffer(d, d);
        Assert.True(days >= 1);
    }

    // ── TimelineWidthPx ──────────────────────────────────────────────────────

    [Fact]
    public void TimelineWidthPx_BelowFloor_ReturnsFloor()
    {
        var px = TimelineGeometry.TimelineWidthPx(totalDays: 10, pixelsPerDay: 4);
        Assert.Equal(TimelineGeometry.MinTimelineWidthPx, px);
    }

    [Fact]
    public void TimelineWidthPx_AboveFloor_ReturnsCeiling()
    {
        var px = TimelineGeometry.TimelineWidthPx(totalDays: 100, pixelsPerDay: 32);
        Assert.Equal(3200, px);
    }

    [Fact]
    public void TimelineWidthPx_ZeroDays_ReturnsFloor()
    {
        Assert.Equal(TimelineGeometry.MinTimelineWidthPx, TimelineGeometry.TimelineWidthPx(0, 32));
    }
}
