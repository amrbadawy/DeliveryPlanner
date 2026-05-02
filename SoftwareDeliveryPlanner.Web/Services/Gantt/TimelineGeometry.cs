namespace SoftwareDeliveryPlanner.Web.Services.Gantt;

/// <summary>
/// Pure date↔pixel and date↔percent geometry for the Gantt timeline.
/// All methods are deterministic and free of DI / DbContext / IMediator.
/// </summary>
public static class TimelineGeometry
{
    /// <summary>Minimum visible timeline width in pixels (legacy 900px - 240px label column).</summary>
    public const int MinTimelineWidthPx = 660;

    /// <summary>Days of buffer added before plan start.</summary>
    public const int PlanStartBufferDays = 2;

    /// <summary>Days of buffer added after plan end.</summary>
    public const int PlanEndBufferDays = 5;

    /// <summary>Returns whether <paramref name="today"/> falls within the inclusive plan range.</summary>
    public static bool IsTodayInPlanRange(DateTime today, DateTime planStart, DateTime planEnd)
    {
        var t = today.Date;
        return t >= planStart.Date && t <= planEnd.Date;
    }

    /// <summary>
    /// Returns the percent (0..100) of the timeline at which <paramref name="date"/> falls,
    /// given the plan's <paramref name="planStart"/> and <paramref name="totalDays"/>.
    /// Rounded to 2 decimal places.
    /// </summary>
    public static double DatePercent(DateTime date, DateTime planStart, int totalDays)
    {
        if (totalDays <= 0) return 0;
        var dayOffset = (date.Date - planStart.Date).TotalDays;
        return Math.Round(dayOffset / totalDays * 100, 2);
    }

    /// <summary>
    /// Computes the (left%, width%) tuple for a bar spanning <paramref name="taskStart"/> to
    /// <paramref name="taskEnd"/>. Width is at least 0.5% to keep the bar visible.
    /// </summary>
    public static (double LeftPct, double WidthPct) BarGeometry(
        DateTime taskStart, DateTime taskEnd, DateTime planStart, int totalDays)
    {
        var leftPct = DatePercent(taskStart, planStart, totalDays);
        var rightPct = DatePercent(taskEnd, planStart, totalDays);
        var widthPct = Math.Max(rightPct - leftPct, 0.5);
        return (leftPct, widthPct);
    }

    /// <summary>
    /// Applies the standard plan-range buffer: <see cref="PlanStartBufferDays"/> before
    /// <paramref name="rawPlanStart"/>, <see cref="PlanEndBufferDays"/> after the later of
    /// <paramref name="rawPlanEnd"/> and any provided <paramref name="strictDeadline"/>.
    /// </summary>
    public static (DateTime PlanStart, DateTime PlanEnd, int TotalDays) ApplyPlanRangeBuffer(
        DateTime rawPlanStart, DateTime rawPlanEnd, DateTime? strictDeadline = null)
    {
        var planEnd = rawPlanEnd;
        if (strictDeadline.HasValue && strictDeadline.Value > planEnd)
            planEnd = strictDeadline.Value;

        var bufferedStart = rawPlanStart.AddDays(-PlanStartBufferDays);
        var bufferedEnd = planEnd.AddDays(PlanEndBufferDays);
        var totalDays = (int)(bufferedEnd - bufferedStart).TotalDays;
        if (totalDays < 1) totalDays = 1;

        return (bufferedStart, bufferedEnd, totalDays);
    }

    /// <summary>
    /// Returns the timeline width in pixels, enforcing the <see cref="MinTimelineWidthPx"/> floor.
    /// </summary>
    public static int TimelineWidthPx(int totalDays, double pixelsPerDay)
    {
        if (totalDays <= 0 || pixelsPerDay <= 0) return MinTimelineWidthPx;
        var raw = (int)Math.Ceiling(totalDays * pixelsPerDay);
        return Math.Max(MinTimelineWidthPx, raw);
    }
}
