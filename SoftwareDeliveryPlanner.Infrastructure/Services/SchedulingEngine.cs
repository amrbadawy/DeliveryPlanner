using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Infrastructure.Data;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal class SchedulingEngine : ISchedulingEngine
{
    // ActivitySource for distributed tracing — registered in Program.cs via AddSource().
    // Produces spans visible in the Aspire dashboard and any OTLP-compatible APM.
    internal static readonly ActivitySource ActivitySource =
        new("SoftwareDeliveryPlanner.SchedulingEngine", "1.0.0");
    private readonly PlannerDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly int _atRiskThreshold;
    private readonly HashSet<DayOfWeek> _weekendDays;
    private readonly bool _ownsContext;

    // In-memory holiday cache — loaded once per RunScheduler call to avoid 1460+ DB queries.
    private List<Holiday>? _holidayCache;

    public SchedulingEngine(PlannerDbContext db, TimeProvider timeProvider, bool ownsContext = false)
    {
        _db = db;
        _timeProvider = timeProvider;
        _ownsContext = ownsContext;
        var setting = _db.Settings.FirstOrDefault(s => s.Key == DomainConstants.SettingKeys.AtRiskThreshold);
        _atRiskThreshold = int.TryParse(setting?.Value, out var t) ? t : 5;

        var weekSetting = _db.Settings.FirstOrDefault(s => s.Key == DomainConstants.SettingKeys.WorkingWeek);
        _weekendDays = DomainConstants.WorkingWeek.GetWeekendDays(weekSetting?.Value ?? DomainConstants.WorkingWeek.SunThu);
    }

    /// <summary>Loads holidays into an in-memory cache if not already loaded.</summary>
    private List<Holiday> GetHolidayCache()
    {
        _holidayCache ??= _db.Holidays.ToList();
        return _holidayCache;
    }

    public bool IsWorkingDay(DateTime date)
    {
        // Check weekend based on the configured working week
        if (_weekendDays.Contains(date.DayOfWeek))
            return false;

        // Check holidays — date falls within any holiday range
        var holidays = GetHolidayCache();
        var isHoliday = holidays.Any(h => h.StartDate.Date <= date.Date && h.EndDate.Date >= date.Date);
        return !isHoliday;
    }

    /// <summary>
    /// Checks if a date is a working day for a specific resource, considering their per-resource WorkingWeek override.
    /// Falls back to global IsWorkingDay if the resource has no override.
    /// </summary>
    private bool IsWorkingDayForResource(DateTime date, TeamMember resource)
    {
        // If resource has a working week override, use it instead of global weekend days
        if (!string.IsNullOrEmpty(resource.WorkingWeek))
        {
            var resourceWeekendDays = DomainConstants.WorkingWeek.GetWeekendDays(resource.WorkingWeek);
            if (resourceWeekendDays.Contains(date.DayOfWeek))
                return false;

            // Still check holidays
            var holidays = GetHolidayCache();
            return !holidays.Any(h => h.StartDate.Date <= date.Date && h.EndDate.Date >= date.Date);
        }

        return IsWorkingDay(date);
    }

    /// <summary>Finds the first holiday whose range covers the given date, or null.</summary>
    public Holiday? GetHolidayForDate(DateTime date)
    {
        var holidays = GetHolidayCache();
        return holidays.FirstOrDefault(h => h.StartDate.Date <= date.Date && h.EndDate.Date >= date.Date);
    }

    public int GetWorkingDaysBetween(DateTime start, DateTime end)
    {
        int count = 0;
        var current = start.Date;
        while (current <= end.Date)
        {
            if (IsWorkingDay(current)) count++;
            current = current.AddDays(1);
        }
        return count;
    }

    private int CalculateSchedulingRank(TaskItem task)
    {
        // Priority 10 = most important → smallest contribution; Priority 1 = least important → largest.
        int priorityComponent = (11 - task.Priority) * 10;

        if (!task.StrictDate.HasValue)
        {
            // Non-strict tasks are always scheduled after all strict-date tasks.
            // int.MaxValue / 2 far exceeds any realistic day-based rank (max ~365k days by year 3000).
            return int.MaxValue / 2 + priorityComponent;
        }

        // Days since 2000-01-01 avoids int overflow entirely.
        // For 2026 dates: ~9600–9900; scaled by 100 so date always dominates priority (range 10–100).
        int daysSinceRef = (int)(task.StrictDate.Value.Date - new DateTime(2000, 1, 1)).TotalDays;
        return daysSinceRef * 100 + priorityComponent;
    }

    /// <summary>Ranks tasks using the deadline_first strategy: nearest strict date first, then least slack.</summary>
    private int CalculateDeadlineFirstRank(TaskItem task)
    {
        int priorityComponent = (11 - task.Priority) * 10;

        if (!task.StrictDate.HasValue)
            return int.MaxValue / 2 + priorityComponent;

        var today = _timeProvider.GetLocalNow().LocalDateTime.Date;
        int slackDays = GetWorkingDaysBetween(today, task.StrictDate.Value);
        // Lower slack = higher priority
        return slackDays * 100 + priorityComponent;
    }

    /// <summary>
    /// Computes the critical path depth for a task (longest chain of dependencies).
    /// Uses a visiting set to detect cycles — cycled tasks return depth 0.
    /// </summary>
    private int ComputeCriticalPathDepth(
        TaskItem task,
        Dictionary<string, TaskItem> taskLookup,
        Dictionary<string, int> depthCache,
        HashSet<string>? visiting = null)
    {
        if (depthCache.TryGetValue(task.TaskId, out var cached))
            return cached;

        visiting ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!visiting.Add(task.TaskId))
            return 0; // cycle detected — break infinite recursion

        int maxDepth = 0;
        foreach (var dep in task.Dependencies)
        {
            if (taskLookup.TryGetValue(dep.PredecessorTaskId, out var predTask))
            {
                int predDepth = ComputeCriticalPathDepth(predTask, taskLookup, depthCache, visiting);
                maxDepth = Math.Max(maxDepth, predDepth + 1);
            }
        }

        visiting.Remove(task.TaskId);
        depthCache[task.TaskId] = maxDepth;
        return maxDepth;
    }

    /// <summary>
    /// Ranks tasks for scheduling based on the chosen strategy.
    /// </summary>
    private IReadOnlyList<TaskItem> RankTasks(List<TaskItem> tasks, string strategy, Dictionary<string, TaskItem> taskLookup)
    {
        switch (strategy)
        {
            case DomainConstants.SchedulingStrategy.DeadlineFirst:
                foreach (var task in tasks)
                    task.ApplySchedulingRank(CalculateDeadlineFirstRank(task));
                return tasks.OrderBy(t => t.SchedulingRank).ToList();

            case DomainConstants.SchedulingStrategy.CriticalPath:
            {
                var depthCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var task in tasks)
                {
                    ComputeCriticalPathDepth(task, taskLookup, depthCache);
                    task.ApplySchedulingRank(CalculateSchedulingRank(task));
                }
                // Deepest dependency chain first, then by standard rank
                return tasks
                    .OrderByDescending(t => depthCache.GetValueOrDefault(t.TaskId))
                    .ThenBy(t => t.SchedulingRank)
                    .ToList();
            }

            case DomainConstants.SchedulingStrategy.BalancedWorkload:
            case DomainConstants.SchedulingStrategy.PriorityFirst:
            default:
                // priority_first and balanced_workload use same task ranking
                // balanced_workload differs only in resource selection
                foreach (var task in tasks)
                    task.ApplySchedulingRank(CalculateSchedulingRank(task));
                return tasks.OrderBy(t => t.SchedulingRank).ToList();
        }
    }

    /// <summary>
    /// Selects the best resource from eligible list based on strategy.
    /// </summary>
    private TeamMember? SelectResource(
        List<TeamMember> eligible,
        TaskItem task,
        string strategy,
        Dictionary<string, double> utilization,
        HashSet<string> preferredSet,
        Dictionary<string, Dictionary<DateTime, double>> usedHours,
        Dictionary<string, double> perResourceHours,
        DateTime calDate)
    {
        if (eligible.Count == 0) return null;

        IOrderedEnumerable<TeamMember> sorted;

        if (string.Equals(strategy, DomainConstants.SchedulingStrategy.BalancedWorkload, StringComparison.OrdinalIgnoreCase))
        {
            // Balanced workload: prefer resource with lowest overall utilization
            sorted = eligible
                .OrderByDescending(r => preferredSet.Contains(r.ResourceId) ? 1 : 0)
                .ThenBy(r => utilization.GetValueOrDefault(r.ResourceId))
                .ThenByDescending(r => DomainConstants.Seniority.Rank.GetValueOrDefault(r.SeniorityLevel));
        }
        else
        {
            // Default: preferred first, then most available today, then seniority tiebreaker
            sorted = eligible
                .OrderByDescending(r => preferredSet.Contains(r.ResourceId) ? 1 : 0)
                .ThenByDescending(r => perResourceHours[r.ResourceId] - (usedHours.GetValueOrDefault(r.ResourceId)?.GetValueOrDefault(calDate) ?? 0))
                .ThenByDescending(r => DomainConstants.Seniority.Rank.GetValueOrDefault(r.SeniorityLevel));
        }

        return sorted.FirstOrDefault();
    }

    /// <summary>
    /// Computes per-resource effective hours for a given date, accounting for availability and adjustments.
    /// Returns a dictionary of ResourceId → available hours for that day.
    /// </summary>
    private Dictionary<string, double> CalculatePerResourceHours(DateTime date, List<TeamMember> resources, List<Adjustment> adjustments)
    {
        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var resource in resources)
        {
            if (resource.Active != DomainConstants.ActiveStatus.Yes) continue;
            if (date < resource.StartDate) continue;
            if (resource.EndDate.HasValue && date > resource.EndDate.Value) continue;

            // Per-resource working day check
            if (!IsWorkingDayForResource(date, resource)) continue;

            double capacity = resource.DailyCapacity * (resource.AvailabilityPct / 100.0);

            // Apply adjustments
            var resourceAdjs = adjustments.Where(a => a.ResourceId == resource.ResourceId).ToList();
            foreach (var adj in resourceAdjs)
            {
                if (adj.AdjStart <= date && adj.AdjEnd >= date)
                {
                    capacity *= adj.AvailabilityPct / 100.0;
                }
            }

            var hours = capacity * DomainConstants.HoursPerDay;
            if (hours > 0)
                result[resource.ResourceId] = hours;
        }

        return result;
    }

    /// <summary>
    /// Computes the total effective capacity in resource-units for a given date (for CalendarDay).
    /// </summary>
    private double CalculateEffectiveCapacity(DateTime date, List<TeamMember> resources, List<Adjustment> adjustments)
    {
        double totalCapacity = 0;

        foreach (var resource in resources)
        {
            if (resource.Active != DomainConstants.ActiveStatus.Yes) continue;
            if (date < resource.StartDate) continue;
            if (resource.EndDate.HasValue && date > resource.EndDate.Value) continue;

            double capacity = resource.DailyCapacity * (resource.AvailabilityPct / 100.0);

            var resourceAdjs = adjustments.Where(a => a.ResourceId == resource.ResourceId).ToList();
            foreach (var adj in resourceAdjs)
            {
                if (adj.AdjStart <= date && adj.AdjEnd >= date)
                {
                    capacity *= adj.AvailabilityPct / 100.0;
                }
            }

            totalCapacity += capacity;
        }

        return totalCapacity;
    }

    private string CalculateRisk(DateTime? plannedFinish, DateTime? strictDate)
    {
        if (!strictDate.HasValue) return DomainConstants.DeliveryRisk.OnTrack;
        if (!plannedFinish.HasValue) return DomainConstants.DeliveryRisk.AtRisk;

        if (plannedFinish.Value > strictDate.Value) return DomainConstants.DeliveryRisk.Late;

        var today = _timeProvider.GetLocalNow().LocalDateTime.Date;
        var workingDaysLeft = GetWorkingDaysBetween(today, strictDate.Value);
        if (workingDaysLeft <= _atRiskThreshold) return DomainConstants.DeliveryRisk.AtRisk;

        return DomainConstants.DeliveryRisk.OnTrack;
    }

    /// <summary>
    /// Adds N working days to a date (for computing lag).
    /// </summary>
    private DateTime AddWorkingDays(DateTime from, int days)
    {
        var current = from;
        int added = 0;
        while (added < days)
        {
            current = current.AddDays(1);
            if (IsWorkingDay(current)) added++;
        }
        return current;
    }

    /// <summary>
    /// Checks if all dependency constraints for a task are met on the given calendar date.
    /// Uses the TaskDependency collection with FS/SS/FF types, lag days, and overlap.
    /// </summary>
    private bool AreDependenciesMet(
        TaskItem task,
        DateTime calDate,
        Dictionary<string, TaskItem> taskLookup,
        Dictionary<string, Dictionary<string, double>> phaseCompletedHours,
        Dictionary<string, Dictionary<string, DateTime?>> phaseStartDate)
    {
        if (task.Dependencies.Count == 0)
            return true;

        foreach (var dep in task.Dependencies)
        {
            if (!taskLookup.TryGetValue(dep.PredecessorTaskId, out var predTask))
                return false; // unknown predecessor = not met

            var predBreakdown = predTask.EffortBreakdown.OrderBy(e => e.SortOrder).ToList();
            if (predBreakdown.Count == 0)
                return false;

            var predTotalHours = predBreakdown.Sum(p => p.EstimationDays * DomainConstants.HoursPerDay);
            var predCompletedHours = predBreakdown.Sum(p =>
                phaseCompletedHours.GetValueOrDefault(dep.PredecessorTaskId)?.GetValueOrDefault(p.Role) ?? 0);

            switch (dep.Type.ToUpperInvariant())
            {
                case DomainConstants.DependencyType.FinishToStart:
                {
                    // Predecessor must be (100 - overlapPct)% complete
                    var requiredPct = (100.0 - dep.OverlapPct) / 100.0;
                    if (predTotalHours > 0 && predCompletedHours < predTotalHours * requiredPct)
                        return false;

                    // If there's lag, find when the required completion was reached and add lag working days
                    if (dep.LagDays > 0)
                    {
                        // Find the latest phase finish date as proxy for when completion was reached
                        var predFinishes = phaseStartDate.GetValueOrDefault(dep.PredecessorTaskId)?
                            .Values.Where(d => d.HasValue).Select(d => d!.Value).ToList();
                        if (predFinishes == null || predFinishes.Count == 0)
                            return false;

                        var completionDate = predFinishes.Max();
                        var earliestStart = AddWorkingDays(completionDate, dep.LagDays);
                        if (calDate < earliestStart)
                            return false;
                    }
                    break;
                }

                case DomainConstants.DependencyType.StartToStart:
                {
                    // Predecessor must have started
                    var predStarts = phaseStartDate.GetValueOrDefault(dep.PredecessorTaskId)?
                        .Values.Where(d => d.HasValue).Select(d => d!.Value).ToList();
                    if (predStarts == null || predStarts.Count == 0)
                        return false;

                    var predStartDate = predStarts.Min();

                    // Current date must be >= predecessor start + lag working days
                    if (dep.LagDays > 0)
                    {
                        var earliestStart = AddWorkingDays(predStartDate, dep.LagDays);
                        if (calDate < earliestStart)
                            return false;
                    }
                    break;
                }

                case DomainConstants.DependencyType.FinishToFinish:
                {
                    // FF: task can start anytime, finish constraint checked post-allocation
                    // Just allow start
                    break;
                }

                default:
                {
                    // Unknown type — treat as FS with full completion required
                    if (predTotalHours > 0 && predCompletedHours < predTotalHours)
                        return false;
                    break;
                }
            }
        }

        return true;
    }

    public string RunScheduler()
    {
        using var activity = ActivitySource.StartActivity("RunScheduler", ActivityKind.Internal);

        // Prime the holiday cache once at the start (eliminates 1460+ individual DB queries)
        _holidayCache = _db.Holidays.ToList();

        var planStartSetting = _db.Settings.FirstOrDefault(s => s.Key == DomainConstants.SettingKeys.PlanStartDate);
        var planStart = DateTime.TryParse(planStartSetting?.Value, out var ps) ? ps : new DateTime(2026, 5, 1);
        var endDate = planStart.AddDays(730);

        // Read scheduling strategy
        var strategySetting = _db.Settings.FirstOrDefault(s => s.Key == DomainConstants.SettingKeys.SchedulingStrategy);
        var strategy = strategySetting?.Value ?? DomainConstants.SchedulingStrategy.PriorityFirst;
        if (!DomainConstants.SchedulingStrategy.All.Contains(strategy))
            strategy = DomainConstants.SchedulingStrategy.PriorityFirst;

        // Read baseline date
        var baselineSetting = _db.Settings.FirstOrDefault(s => s.Key == DomainConstants.SettingKeys.BaselineDate);
        DateTime? baselineDate = DateTime.TryParse(baselineSetting?.Value, out var bd) ? bd : null;

        var tasks = _db.Tasks
            .Include(t => t.EffortBreakdown)
            .Include(t => t.Dependencies)
            .AsSplitQuery()
            .ToList();
        var resources = _db.Resources.ToList();
        var adjustments = _db.Adjustments.ToList();

        if (!tasks.Any()) return "No tasks to schedule";

        // Build task lookup for dependency checking
        var taskLookup = tasks.ToDictionary(t => t.TaskId, StringComparer.OrdinalIgnoreCase);

        // Rank tasks using strategy
        var rankedTasks = RankTasks(tasks, strategy, taskLookup);
        tasks = rankedTasks.ToList();

        // Clear and generate calendar
        _db.Calendar.RemoveRange(_db.Calendar);

        var calendar = new List<CalendarDay>();
        var current = planStart;
        var dateKey = 1;

        while (current <= endDate)
        {
            var isWorking = IsWorkingDay(current);
            var holiday = GetHolidayForDate(current);

            var baseCap = isWorking ? CalculateEffectiveCapacity(current, resources, adjustments) : 0;

            calendar.Add(new CalendarDay
            {
                DateKey = dateKey,
                CalendarDate = current,
                DayName = current.ToString("dddd"),
                IsWorkingDay = isWorking,
                IsHoliday = holiday != null,
                HolidayName = holiday?.HolidayName,
                BaseCapacity = baseCap,
                AdjCapacity = baseCap,
                EffectiveCapacity = baseCap,
                ReservedCapacity = 0,
                RemainingCapacity = baseCap
            });

            current = current.AddDays(1);
            dateKey++;
        }
        _db.Calendar.AddRange(calendar);
        _db.SaveChanges();

        // Baseline freeze: only remove unlocked allocations
        _db.Allocations.RemoveRange(_db.Allocations.Where(a => !a.IsLocked));

        // Load locked allocations to pre-compute cumulative state
        var lockedAllocations = _db.Allocations.Where(a => a.IsLocked).ToList();

        var allocations = new List<Allocation>();
        var allocCounter = 1;

        // State tracking
        // usedHours[resourceId][date] = hours already consumed
        var usedHours = new Dictionary<string, Dictionary<DateTime, double>>(StringComparer.OrdinalIgnoreCase);
        // phaseCompletedHours[taskId][role] = hours completed
        var phaseCompletedHours = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
        // phaseStartDate[taskId][role] = first allocation date
        var phaseStartDate = new Dictionary<string, Dictionary<string, DateTime?>>(StringComparer.OrdinalIgnoreCase);
        // phaseFinishDate[taskId][role] = last allocation date when phase completes
        var phaseFinishDate = new Dictionary<string, Dictionary<string, DateTime?>>(StringComparer.OrdinalIgnoreCase);
        // taskResourceIds[taskId] = set of assigned resource IDs
        var taskResourceIds = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        // Build resource lookup by role — only include resources whose active
        // period overlaps the scheduling window [planStart, endDate].  Resources
        // that join after the plan ends or leave before it starts can never be
        // allocated, so excluding them lets the auto-skip logic fire correctly.
        var resourcesByRole = resources
            .Where(r => r.Active == DomainConstants.ActiveStatus.Yes)
            .Where(r => r.StartDate <= endDate)
            .Where(r => !r.EndDate.HasValue || r.EndDate.Value >= planStart)
            .GroupBy(r => r.Role, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // Initialize state for all tasks
        foreach (var task in tasks)
        {
            phaseCompletedHours[task.TaskId] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            phaseStartDate[task.TaskId] = new Dictionary<string, DateTime?>(StringComparer.OrdinalIgnoreCase);
            phaseFinishDate[task.TaskId] = new Dictionary<string, DateTime?>(StringComparer.OrdinalIgnoreCase);
            taskResourceIds[task.TaskId] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var phase in task.EffortBreakdown)
            {
                phaseCompletedHours[task.TaskId][phase.Role] = 0;
                phaseStartDate[task.TaskId][phase.Role] = null;
                phaseFinishDate[task.TaskId][phase.Role] = null;
            }
        }

        // Pre-compute cumulative hours from locked allocations
        foreach (var locked in lockedAllocations)
        {
            // Update used hours
            if (!usedHours.ContainsKey(locked.ResourceId))
                usedHours[locked.ResourceId] = new Dictionary<DateTime, double>();
            if (!usedHours[locked.ResourceId].ContainsKey(locked.CalendarDate))
                usedHours[locked.ResourceId][locked.CalendarDate] = 0;
            usedHours[locked.ResourceId][locked.CalendarDate] += locked.HoursAllocated;

            // Update phase state
            if (phaseCompletedHours.ContainsKey(locked.TaskId) &&
                phaseCompletedHours[locked.TaskId].ContainsKey(locked.Role))
            {
                phaseCompletedHours[locked.TaskId][locked.Role] += locked.HoursAllocated;

                if (!phaseStartDate[locked.TaskId][locked.Role].HasValue ||
                    locked.CalendarDate < phaseStartDate[locked.TaskId][locked.Role])
                    phaseStartDate[locked.TaskId][locked.Role] = locked.CalendarDate;

                if (locked.IsComplete)
                    phaseFinishDate[locked.TaskId][locked.Role] = locked.CalendarDate;
            }

            if (taskResourceIds.ContainsKey(locked.TaskId))
                taskResourceIds[locked.TaskId].Add(locked.ResourceId);
        }

        // Auto-complete phases where no active resources exist for the role.
        // Without this, the overlap constraint (line ~648) permanently blocks
        // downstream phases when a preceding phase can never be staffed.
        foreach (var task in tasks)
        {
            foreach (var phase in task.EffortBreakdown)
            {
                if (phase.EstimationDays > 0 && !resourcesByRole.ContainsKey(phase.Role))
                {
                    var totalHours = phase.EstimationDays * DomainConstants.HoursPerDay;
                    phaseCompletedHours[task.TaskId][phase.Role] = totalHours;
                }
            }
        }

        // Overall utilization tracking for balanced_workload strategy
        var overallUtilization = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var resource in resources.Where(r => r.Active == DomainConstants.ActiveStatus.Yes))
        {
            overallUtilization[resource.ResourceId] = 0;
        }
        // Seed utilization from locked allocations
        foreach (var locked in lockedAllocations)
        {
            if (overallUtilization.ContainsKey(locked.ResourceId))
                overallUtilization[locked.ResourceId] += locked.HoursAllocated;
        }

        // Track which resources are allocated to which task on which day (for parallel phase guard)
        // taskDayResources[taskId][date] = set of resourceIds already allocated on that day
        var taskDayResources = new Dictionary<string, Dictionary<DateTime, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var task in tasks)
            taskDayResources[task.TaskId] = new Dictionary<DateTime, HashSet<string>>();

        // Seed from locked allocations
        foreach (var locked in lockedAllocations)
        {
            if (!taskDayResources.ContainsKey(locked.TaskId))
                taskDayResources[locked.TaskId] = new Dictionary<DateTime, HashSet<string>>();
            if (!taskDayResources[locked.TaskId].ContainsKey(locked.CalendarDate))
                taskDayResources[locked.TaskId][locked.CalendarDate] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            taskDayResources[locked.TaskId][locked.CalendarDate].Add(locked.ResourceId);
        }

        foreach (var calRow in calendar.Where(c => c.IsWorkingDay))
        {
            var calDate = calRow.CalendarDate;
            var calDateKey = calRow.DateKey;

            // Baseline freeze: skip days before baseline_date
            if (baselineDate.HasValue && calDate < baselineDate.Value.Date)
                continue;

            // Compute per-resource available hours for this day
            var perResourceHours = CalculatePerResourceHours(calDate, resources, adjustments);

            foreach (var task in tasks)
            {
                var taskId = task.TaskId;
                var breakdown = task.EffortBreakdown.OrderBy(e => e.SortOrder).ToList();

                if (breakdown.Count == 0) continue;

                // Skip if all phases complete
                var allPhasesComplete = breakdown.All(p =>
                    phaseCompletedHours[taskId].GetValueOrDefault(p.Role) >= p.EstimationDays * DomainConstants.HoursPerDay);
                if (allPhasesComplete) continue;

                // Skip if before OverrideStart
                if (task.OverrideStart.HasValue && calDate < task.OverrideStart.Value.Date)
                    continue;

                // Check dependency constraints using TaskDependency collection with FS/SS/FF support
                if (!AreDependenciesMet(task, calDate, taskLookup, phaseCompletedHours, phaseStartDate))
                    continue;

                // Parse preferred resources once per task
                var preferredSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(task.PreferredResourceIds))
                {
                    foreach (var pref in task.PreferredResourceIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        preferredSet.Add(pref);
                    }
                }

                // Parallel phase execution: iterate all incomplete phases instead of breaking on first
                foreach (var phase in breakdown)
                {
                    var phaseIndex = breakdown.IndexOf(phase);
                    var totalPhaseHours = phase.EstimationDays * DomainConstants.HoursPerDay;
                    var completedHours = phaseCompletedHours[taskId].GetValueOrDefault(phase.Role);

                    if (completedHours >= totalPhaseHours)
                        continue; // phase already complete

                    // Check overlap constraint with previous phase
                    if (phaseIndex > 0)
                    {
                        var prevPhase = breakdown[phaseIndex - 1];
                        var prevCompleted = phaseCompletedHours[taskId].GetValueOrDefault(prevPhase.Role);
                        var prevTotal = prevPhase.EstimationDays * DomainConstants.HoursPerDay;
                        var requiredPct = (100.0 - phase.OverlapPct) / 100.0;
                        if (prevCompleted < prevTotal * requiredPct)
                            continue; // can't start this phase yet
                    }

                    // This is an active phase — allocate resources
                    var remainingHours = totalPhaseHours - phaseCompletedHours[taskId].GetValueOrDefault(phase.Role);

                    // Find eligible resources matching phase.Role
                    List<TeamMember> eligible;
                    if (resourcesByRole.TryGetValue(phase.Role, out var roleResources))
                    {
                        eligible = roleResources.Where(r =>
                        {
                            if (!perResourceHours.ContainsKey(r.ResourceId)) return false;
                            var used = usedHours.GetValueOrDefault(r.ResourceId)?.GetValueOrDefault(calDate) ?? 0;
                            var available = perResourceHours[r.ResourceId] - used;
                            if (available < DomainConstants.MinAllocationHours) return false;

                            // Per-resource working day check
                            if (!IsWorkingDayForResource(calDate, r)) return false;

                            // Guard: don't allocate same resource to two phases of same task on same day
                            if (taskDayResources.TryGetValue(taskId, out var dayMap) &&
                                dayMap.TryGetValue(calDate, out var dayResources) &&
                                dayResources.Contains(r.ResourceId))
                                return false;

                            // Seniority filter: if phase has MinSeniority, only include resources meeting the requirement
                            if (!string.IsNullOrEmpty(phase.MinSeniority))
                            {
                                var requiredRank = DomainConstants.Seniority.Rank.GetValueOrDefault(phase.MinSeniority);
                                var resourceRank = DomainConstants.Seniority.Rank.GetValueOrDefault(r.SeniorityLevel);
                                if (resourceRank < requiredRank)
                                    return false;
                            }

                            return true;
                        }).ToList();
                    }
                    else
                    {
                        eligible = new List<TeamMember>();
                    }

                    int resourcesAssigned = 0;
                    var maxResources = (int)Math.Ceiling(phase.MaxFte);

                    while (eligible.Count > 0 && resourcesAssigned < maxResources && remainingHours > 0)
                    {
                        var resource = SelectResource(eligible, task, strategy, overallUtilization, preferredSet, usedHours, perResourceHours, calDate);
                        if (resource == null) break;

                        eligible.Remove(resource);

                        var used = usedHours.GetValueOrDefault(resource.ResourceId)?.GetValueOrDefault(calDate) ?? 0;
                        var available = perResourceHours[resource.ResourceId] - used;
                        var give = Math.Min(available, remainingHours);
                        if (give >= DomainConstants.MinAllocationHours)
                            give = DomainConstants.MinAllocationHours * Math.Floor(give / DomainConstants.MinAllocationHours);
                        else if (remainingHours < DomainConstants.MinAllocationHours && give >= remainingHours)
                            give = remainingHours; // allow final fractional allocation to complete phase
                        else
                            continue;

                        var newCumulative = phaseCompletedHours[taskId].GetValueOrDefault(phase.Role) + give;
                        var isPhaseComplete = newCumulative >= totalPhaseHours;

                        var newAlloc = new Allocation
                        {
                            AllocationId = $"ALLOC-{allocCounter++:D6}",
                            TaskId = taskId,
                            ResourceId = resource.ResourceId,
                            Role = phase.Role,
                            DateKey = calDateKey,
                            CalendarDate = calDate,
                            SchedRank = task.SchedulingRank,
                            HoursAllocated = give,
                            CumulativeEffort = newCumulative,
                            IsComplete = isPhaseComplete,
                            ServiceStatus = isPhaseComplete ? DomainConstants.TaskStatus.Completed : DomainConstants.TaskStatus.InProgress
                        };

                        allocations.Add(newAlloc);

                        // Update used hours
                        if (!usedHours.ContainsKey(resource.ResourceId))
                            usedHours[resource.ResourceId] = new Dictionary<DateTime, double>();
                        if (!usedHours[resource.ResourceId].ContainsKey(calDate))
                            usedHours[resource.ResourceId][calDate] = 0;
                        usedHours[resource.ResourceId][calDate] += give;

                        // Update overall utilization
                        if (overallUtilization.ContainsKey(resource.ResourceId))
                            overallUtilization[resource.ResourceId] += give;

                        phaseCompletedHours[taskId][phase.Role] += give;
                        taskResourceIds[taskId].Add(resource.ResourceId);
                        remainingHours -= give;
                        resourcesAssigned++;

                        // Track task-day-resource for parallel phase guard
                        if (!taskDayResources[taskId].ContainsKey(calDate))
                            taskDayResources[taskId][calDate] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        taskDayResources[taskId][calDate].Add(resource.ResourceId);

                        if (!phaseStartDate[taskId][phase.Role].HasValue)
                            phaseStartDate[taskId][phase.Role] = calDate;

                        if (remainingHours <= 0)
                        {
                            phaseFinishDate[taskId][phase.Role] = calDate;
                            break;
                        }
                    }

                    // No break here — continue to check next phases (parallel phase execution)
                }
            }
        }

        _db.Allocations.AddRange(allocations);

        // Combine locked + new allocations for task result computation
        var allAllocations = lockedAllocations.Concat(allocations).ToList();

        // Update tasks with scheduling results
        foreach (var task in tasks)
        {
            var taskId = task.TaskId;
            var breakdown = task.EffortBreakdown.OrderBy(e => e.SortOrder).ToList();

            var starts = phaseStartDate.GetValueOrDefault(taskId)?.Values.Where(d => d.HasValue).Select(d => d!.Value).ToList()
                ?? new List<DateTime>();
            var finishes = phaseFinishDate.GetValueOrDefault(taskId)?.Values.Where(d => d.HasValue).Select(d => d!.Value).ToList()
                ?? new List<DateTime>();

            // Safety net: include last allocation date for phases that have allocations
            // but didn't formally complete (e.g., due to rounding or resource shortage)
            var lastAllocByRole = allAllocations
                .Where(a => a.TaskId.Equals(taskId, StringComparison.OrdinalIgnoreCase))
                .GroupBy(a => a.Role, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Max(a => a.CalendarDate))
                .ToList();
            finishes.AddRange(lastAllocByRole);

            DateTime? plannedStart = starts.Any() ? starts.Min() : null;
            DateTime? plannedFinish = finishes.Any() ? finishes.Max() : null;

            // Check if all phases are complete
            var allComplete = breakdown.Count > 0 && breakdown.All(p =>
                phaseCompletedHours.GetValueOrDefault(taskId)?.GetValueOrDefault(p.Role) >= p.EstimationDays * DomainConstants.HoursPerDay);

            string status;
            if (!plannedStart.HasValue)
                status = DomainConstants.TaskStatus.NotStarted;
            else if (allComplete)
                status = DomainConstants.TaskStatus.Completed;
            else
                status = DomainConstants.TaskStatus.InProgress;

            var resIds = taskResourceIds.GetValueOrDefault(taskId) ?? new HashSet<string>();
            var assignedResourceId = resIds.Count > 0 ? string.Join(",", resIds.OrderBy(r => r)) : null;

            // Peak resource count: max resources allocated on any single day
            var taskAllocs = allAllocations.Where(a => a.TaskId.Equals(taskId, StringComparison.OrdinalIgnoreCase)).ToList();
            var peakResource = taskAllocs.Count > 0
                ? taskAllocs.GroupBy(a => a.CalendarDate).Max(g => g.Count())
                : 0;

            int duration = 0;
            if (plannedStart.HasValue && plannedFinish.HasValue)
                duration = GetWorkingDaysBetween(plannedStart.Value, plannedFinish.Value);

            string risk = CalculateRisk(plannedFinish, task.StrictDate);

            task.ApplySchedulingResult(
                peakConcurrency: peakResource,
                plannedStart: plannedStart,
                plannedFinish: plannedFinish,
                duration: duration,
                status: status,
                deliveryRisk: risk,
                assignedResourceId: assignedResourceId);
        }

        // Update calendar reserved/remaining capacity
        foreach (var calRow in calendar)
        {
            var dayAllocs = allAllocations.Where(a => a.CalendarDate.Date == calRow.CalendarDate.Date).ToList();
            var reservedHours = dayAllocs.Sum(a => a.HoursAllocated);
            calRow.ReservedCapacity = reservedHours / DomainConstants.HoursPerDay;
            calRow.RemainingCapacity = calRow.EffectiveCapacity - calRow.ReservedCapacity;

            var existing = _db.Calendar.First(c => c.DateKey == calRow.DateKey);
            existing.ReservedCapacity = calRow.ReservedCapacity;
            existing.RemainingCapacity = calRow.RemainingCapacity;
        }

        _db.SaveChanges();

        var resultMessage = $"Successfully scheduled {tasks.Count} tasks with {allocations.Count} allocations";

        // Tag the trace span with scheduling outcome — visible in Aspire dashboard and APM tools.
        activity?.SetTag("scheduling.task_count", tasks.Count);
        activity?.SetTag("scheduling.allocation_count", allocations.Count);
        activity?.SetTag("scheduling.status", "success");
        activity?.SetTag("scheduling.strategy", strategy);

        return resultMessage;
    }

    public ScheduleDiffDto PreviewSchedule()
    {
        using var activity = ActivitySource.StartActivity("PreviewSchedule", ActivityKind.Internal);

        // 1. Snapshot current task state before any changes (AsNoTracking so EF stays clean)
        var tasks = _db.Tasks.Include(t => t.EffortBreakdown).Include(t => t.Dependencies).AsSplitQuery().AsNoTracking().ToList();
        var snapshots = tasks.Select(t => new
        {
            t.TaskId,
            t.ServiceName,
            OldStart = t.PlannedStart,
            OldFinish = t.PlannedFinish,
            OldRisk = t.DeliveryRisk,
            OldAssignedResourceId = t.AssignedResourceId
        }).ToDictionary(s => s.TaskId, StringComparer.OrdinalIgnoreCase);

        // 2. Run the full scheduler inside a transaction that we will roll back,
        //    so the DB is left exactly as it was — true dry-run.
        using var transaction = _db.Database.BeginTransaction();
        try
        {
            RunScheduler();

            // 3. Detach tracked entities so the next reads go to the DB (still in transaction)
            foreach (var entry in _db.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;

            var updatedTasks = _db.Tasks.Include(t => t.EffortBreakdown).Include(t => t.Dependencies).AsSplitQuery().AsNoTracking().ToList();
            var newAllocCount = _db.Allocations.AsNoTracking().Count(a => !a.IsLocked);

            // 4. Compute diffs
            var changes = new List<TaskDiffEntry>();
            int affected = 0;
            int unchanged = 0;

            foreach (var newTask in updatedTasks)
            {
                if (!snapshots.TryGetValue(newTask.TaskId, out var old))
                {
                    changes.Add(new TaskDiffEntry(
                        newTask.TaskId, newTask.ServiceName,
                        null, newTask.PlannedStart,
                        null, newTask.PlannedFinish,
                        null, newTask.DeliveryRisk,
                        null, newTask.AssignedResourceId,
                        "Added"));
                    affected++;
                    continue;
                }

                bool startChanged = old.OldStart != newTask.PlannedStart;
                bool finishChanged = old.OldFinish != newTask.PlannedFinish;
                bool riskChanged = !string.Equals(old.OldRisk, newTask.DeliveryRisk, StringComparison.OrdinalIgnoreCase);
                bool resourceChanged = !string.Equals(old.OldAssignedResourceId, newTask.AssignedResourceId, StringComparison.OrdinalIgnoreCase);

                if (startChanged || finishChanged || riskChanged || resourceChanged)
                {
                    string changeType = "Modified";
                    if (riskChanged && string.Equals(newTask.DeliveryRisk, DomainConstants.DeliveryRisk.Late, StringComparison.OrdinalIgnoreCase))
                        changeType = "NowLate";
                    else if (riskChanged && string.Equals(old.OldRisk, DomainConstants.DeliveryRisk.Late, StringComparison.OrdinalIgnoreCase))
                        changeType = "NoLongerLate";

                    changes.Add(new TaskDiffEntry(
                        newTask.TaskId, newTask.ServiceName,
                        old.OldStart, newTask.PlannedStart,
                        old.OldFinish, newTask.PlannedFinish,
                        old.OldRisk, newTask.DeliveryRisk,
                        old.OldAssignedResourceId, newTask.AssignedResourceId,
                        changeType));
                    affected++;
                }
                else
                {
                    unchanged++;
                }
            }

            var result = new ScheduleDiffDto(changes, affected, unchanged, newAllocCount);

            // 5. Roll back — undoes all RunScheduler() DB writes (true dry-run)
            transaction.Rollback();

            // 6. Detach all tracked entities so the DbContext is clean for future use
            foreach (var entry in _db.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;

            return result;
        }
        catch
        {
            transaction.Rollback();
            foreach (var entry in _db.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;
            throw;
        }
    }

    public Dictionary<string, object> GetDashboardKPIs()
    {
        var tasks = _db.Tasks.Include(t => t.EffortBreakdown).AsSplitQuery().ToList();
        var resources = _db.Resources.Where(r => r.Active == DomainConstants.ActiveStatus.Yes).ToList();

        var totalServices = tasks.Count;
        var totalEstimation = tasks.Sum(t => t.TotalEstimationDays);
        var totalCapacity = resources.Sum(r => r.DailyCapacity * r.AvailabilityPct / 100.0);
        var activeResources = resources.Count;

        var finishDates = tasks.Where(t => t.PlannedFinish.HasValue).Select(t => t.PlannedFinish!.Value).ToList();
        var overallFinish = finishDates.Any() ? finishDates.Max() : (DateTime?)null;

        var startDates = tasks.Where(t => t.PlannedStart.HasValue).Select(t => t.PlannedStart!.Value).ToList();
        var earliestStart = startDates.Any() ? startDates.Min() : (DateTime?)null;

        var strictCount = tasks.Count(t => t.StrictDate.HasValue);
        var scheduledTasks = tasks.Where(t => t.PlannedStart.HasValue && t.PlannedFinish.HasValue).ToList();
        var onTrack = scheduledTasks.Count(t => t.DeliveryRisk == DomainConstants.DeliveryRisk.OnTrack);
        var atRisk = scheduledTasks.Count(t => t.DeliveryRisk == DomainConstants.DeliveryRisk.AtRisk);
        var late = scheduledTasks.Count(t => t.DeliveryRisk == DomainConstants.DeliveryRisk.Late);
        var unscheduled = tasks.Count - scheduledTasks.Count;

        var assignedResources = tasks.Where(t => t.PeakConcurrency.HasValue && t.PeakConcurrency > 0).Select(t => t.PeakConcurrency!.Value).ToList();
        var avgAssigned = assignedResources.Any() ? assignedResources.Average() : 0;

        var today = _timeProvider.GetLocalNow().LocalDateTime.Date;
        var upcomingStrict = tasks.Where(t => t.StrictDate.HasValue && t.StrictDate >= today)
            .OrderBy(t => t.StrictDate)
            .Take(5)
            .ToList();

        // Count overallocations: days where a resource's total allocated hours exceed 8
        var allocations = _db.Allocations.ToList();
        var overallocationCount = allocations
            .GroupBy(a => new { a.ResourceId, a.CalendarDate })
            .Count(g => g.Sum(a => a.HoursAllocated) > 8);

        return new Dictionary<string, object>
        {
            ["total_services"] = totalServices,
            ["total_estimation"] = totalEstimation,
            ["total_capacity"] = totalCapacity,
            ["active_resources"] = activeResources,
            ["earliest_start"] = earliestStart ?? DateTime.MinValue,
            ["overall_finish"] = overallFinish ?? DateTime.MinValue,
            ["strict_count"] = strictCount,
            ["on_track"] = onTrack,
            ["at_risk"] = atRisk,
            ["late"] = late,
            ["unscheduled"] = unscheduled,
            ["avg_assigned"] = Math.Round(avgAssigned, 1),
            ["upcoming_strict"] = upcomingStrict,
            ["overallocation_count"] = overallocationCount
        };
    }

    public List<OutputPlanRowDto> GetOutputPlan()
    {
        var tasks = _db.Tasks.Include(t => t.EffortBreakdown).AsSplitQuery().OrderBy(t => t.SchedulingRank).ToList();
        var output = new List<OutputPlanRowDto>();

        for (int i = 0; i < tasks.Count; i++)
        {
            var task = tasks[i];
            output.Add(new OutputPlanRowDto(
                Num: i + 1,
                TaskId: task.TaskId,
                ServiceName: task.ServiceName,
                AssignedResourceIds: task.AssignedResourceId,
                PlannedStart: task.PlannedStart?.ToString("yyyy-MM-dd"),
                PlannedFinish: task.PlannedFinish?.ToString("yyyy-MM-dd"),
                Duration: task.Duration,
                TotalEstimationDays: task.TotalEstimationDays,
                StrictDate: task.StrictDate?.ToString("yyyy-MM-dd"),
                Status: task.Status,
                DeliveryRisk: task.DeliveryRisk,
                Phase: task.Phase));
        }

        return output;
    }

    public void Dispose()
    {
        if (_ownsContext)
            _db.Dispose();
    }

    public void FreezeBaseline()
    {
        // Lock all currently-unlocked allocations so they survive future scheduler runs
        var unlocked = _db.Allocations.Where(a => !a.IsLocked).ToList();
        foreach (var a in unlocked)
            a.IsLocked = true;

        // Upsert baseline_date = today
        var today = _timeProvider.GetUtcNow().Date.ToString("yyyy-MM-dd");
        var setting = _db.Settings.FirstOrDefault(s => s.Key == DomainConstants.SettingKeys.BaselineDate);
        if (setting is not null)
            setting.Value = today;
        else
            _db.Settings.Add(new Setting { Key = DomainConstants.SettingKeys.BaselineDate, Value = today });

        _db.SaveChanges();
    }
}
