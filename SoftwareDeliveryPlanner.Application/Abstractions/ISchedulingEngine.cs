using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Application.Abstractions;

/// <summary>
/// Abstraction over the scheduling engine, enabling testability
/// and decoupling services from the concrete implementation.
/// Instances created by <see cref="ISchedulingEngineFactory"/> own their
/// database context and must be disposed after use.
/// </summary>
public interface ISchedulingEngine : IDisposable
{
    /// <summary>Runs the full scheduling algorithm. Returns a summary message.</summary>
    string RunScheduler();

    /// <summary>Computes dashboard KPI metrics from current task/resource data.</summary>
    Dictionary<string, object> GetDashboardKPIs();

    /// <summary>Produces the output plan rows for all tasks ordered by scheduling rank.</summary>
    List<OutputPlanRowDto> GetOutputPlan();

    /// <summary>Determines whether the given date is a working day (not a weekend or holiday).</summary>
    bool IsWorkingDay(DateTime date);

    /// <summary>Counts the number of working days (inclusive) between two dates.</summary>
    int GetWorkingDaysBetween(DateTime start, DateTime end);

    /// <summary>Finds the first holiday covering the given date, or null.</summary>
    Holiday? GetHolidayForDate(DateTime date);

    /// <summary>Runs the scheduler in dry-run mode and returns a diff of what would change.</summary>
    ScheduleDiffDto PreviewSchedule();

    /// <summary>
    /// Locks all current unlocked allocations as the frozen baseline
    /// and records today as the baseline_date setting.
    /// </summary>
    void FreezeBaseline();
}
