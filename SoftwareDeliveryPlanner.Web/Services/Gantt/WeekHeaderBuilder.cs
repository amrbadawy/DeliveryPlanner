using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Services;

namespace SoftwareDeliveryPlanner.Web.Services.Gantt;

/// <summary>One column in the Gantt week-header row.</summary>
public sealed record WeekColumn(
    int WeekNumber,
    int Year,
    DateTime Start,
    DateTime End,
    double LeftPct,
    double WidthPct,
    string Label,
    string DateRange,
    string FullLabel);

/// <summary>One column in the Gantt month-header row.</summary>
public sealed record MonthColumn(
    string Label,
    double LeftPct,
    double WidthPct);

/// <summary>
/// Pure header builder for the Gantt timeline. Builds week and month column models from
/// plan range + working-week / week-numbering settings. No DI / DbContext / IMediator.
/// </summary>
public static class WeekHeaderBuilder
{
    /// <summary>
    /// Builds week columns covering <paramref name="planStart"/>..<paramref name="planEnd"/>.
    /// The first column starts at the first <paramref name="workingWeekCode"/>'s
    /// week-start day on or after <paramref name="planStart"/>. Subsequent columns are 7 days each,
    /// truncated at <paramref name="planEnd"/>. Week numbers are computed via
    /// <see cref="WeekNumberCalculator.GetWeekOfYear"/> using <paramref name="weekNumberingCode"/>.
    /// </summary>
    public static IReadOnlyList<WeekColumn> BuildWeeks(
        DateTime planStart,
        DateTime planEnd,
        int totalDays,
        string workingWeekCode,
        string weekNumberingCode)
    {
        if (totalDays <= 0 || planEnd < planStart)
            return Array.Empty<WeekColumn>();

        var weekStartDay = DomainConstants.WorkingWeek.GetWeekStartDay(workingWeekCode);

        // First week-start day on or after planStart.
        var current = planStart.Date;
        while (current.DayOfWeek != weekStartDay)
            current = current.AddDays(1);

        var result = new List<WeekColumn>();
        while (current <= planEnd.Date)
        {
            var weekEnd = current.AddDays(6);
            if (weekEnd > planEnd.Date) weekEnd = planEnd.Date;

            var leftPct = TimelineGeometry.DatePercent(current, planStart, totalDays);
            var rightPct = TimelineGeometry.DatePercent(weekEnd, planStart, totalDays);

            var weekNumber = WeekNumberCalculator.GetWeekOfYear(current, weekNumberingCode, workingWeekCode);
            var dateRange = $"{current:dd MMM} - {weekEnd:dd MMM}";

            result.Add(new WeekColumn(
                WeekNumber: weekNumber,
                Year: current.Year,
                Start: current,
                End: weekEnd,
                LeftPct: Math.Round(leftPct, 2),
                WidthPct: Math.Round(Math.Max(rightPct - leftPct, 0.5), 2),
                Label: $"W{weekNumber}",
                DateRange: dateRange,
                FullLabel: $"Week {weekNumber}, {current:yyyy} ({dateRange})"));

            current = current.AddDays(7);
        }

        return result;
    }

    /// <summary>Builds month columns covering the plan range. Each column spans one calendar month.</summary>
    public static IReadOnlyList<MonthColumn> BuildMonths(
        DateTime planStart, DateTime planEnd, int totalDays)
    {
        if (totalDays <= 0 || planEnd < planStart) return Array.Empty<MonthColumn>();

        var result = new List<MonthColumn>();
        var current = new DateTime(planStart.Year, planStart.Month, 1);
        while (current <= planEnd.Date)
        {
            var monthEnd = current.AddMonths(1).AddDays(-1);
            if (monthEnd > planEnd.Date) monthEnd = planEnd.Date;

            var start = current < planStart ? planStart : current;
            var leftPct = TimelineGeometry.DatePercent(start, planStart, totalDays);
            var rightPct = TimelineGeometry.DatePercent(monthEnd, planStart, totalDays);

            result.Add(new MonthColumn(
                Label: current.ToString("MMM yyyy"),
                LeftPct: Math.Round(leftPct, 2),
                WidthPct: Math.Round(Math.Max(rightPct - leftPct, 1), 2)));

            current = current.AddMonths(1);
        }

        return result;
    }
}
