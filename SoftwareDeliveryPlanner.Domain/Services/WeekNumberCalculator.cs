using System.Globalization;

namespace SoftwareDeliveryPlanner.Domain.Services;

/// <summary>
/// Centralized week-of-year calculator used across all screens (Gantt, ScenarioGantt,
/// Heatmap, UtilizationForecast). Always uses the Gregorian calendar so plan dates
/// and labels stay consistent regardless of the user's culture.
///
/// The numbering rule is driven by <see cref="DomainConstants.SettingKeys.WeekNumbering"/>.
/// When the rule is <see cref="DomainConstants.WeekNumbering.FollowWorkingWeek"/>,
/// the result derives from the WorkingWeek setting (SUN_THU → Sunday-start FirstDay,
/// MON_FRI → ISO 8601-equivalent).
/// </summary>
public static class WeekNumberCalculator
{
    private static readonly Calendar GregorianCal = new GregorianCalendar();

    /// <summary>Returns the real calendar week-of-year for the given date.</summary>
    public static int GetWeekOfYear(DateTime date, string? weekNumberingCode, string? workingWeekCode)
    {
        var (rule, firstDay) = Resolve(weekNumberingCode, workingWeekCode);
        return GregorianCal.GetWeekOfYear(date, rule, firstDay);
    }

    /// <summary>Returns the compact label for the date, e.g. "W18".</summary>
    public static string GetWeekLabel(DateTime date, string? weekNumberingCode, string? workingWeekCode)
        => $"W{GetWeekOfYear(date, weekNumberingCode, workingWeekCode)}";

    /// <summary>
    /// Resolves the (CalendarWeekRule, DayOfWeek) pair to use for week-of-year calculation.
    /// Falls back safely to FOLLOW_WORKING_WEEK behaviour for unknown codes.
    /// </summary>
    public static (CalendarWeekRule Rule, DayOfWeek FirstDay) Resolve(string? weekNumberingCode, string? workingWeekCode)
    {
        var code = string.IsNullOrWhiteSpace(weekNumberingCode)
            ? DomainConstants.WeekNumbering.Default
            : weekNumberingCode!;

        return code switch
        {
            DomainConstants.WeekNumbering.Iso8601
                => (CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday),

            DomainConstants.WeekNumbering.SundayFirstDay
                => (CalendarWeekRule.FirstDay, DayOfWeek.Sunday),

            DomainConstants.WeekNumbering.MondayFirstDay
                => (CalendarWeekRule.FirstDay, DayOfWeek.Monday),

            DomainConstants.WeekNumbering.FollowWorkingWeek
                => ResolveFromWorkingWeek(workingWeekCode),

            // Unknown / future codes: same safe default as missing setting.
            _ => ResolveFromWorkingWeek(workingWeekCode)
        };
    }

    private static (CalendarWeekRule Rule, DayOfWeek FirstDay) ResolveFromWorkingWeek(string? workingWeekCode)
    {
        return workingWeekCode switch
        {
            DomainConstants.WorkingWeek.MonFri
                => (CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday), // ISO-equivalent
            DomainConstants.WorkingWeek.SunThu
                => (CalendarWeekRule.FirstDay, DayOfWeek.Sunday),
            _ => (CalendarWeekRule.FirstDay, DayOfWeek.Sunday) // matches WorkingWeek default (SUN_THU)
        };
    }
}
