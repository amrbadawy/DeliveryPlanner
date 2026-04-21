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

    public string RunScheduler()
    {
        using var activity = ActivitySource.StartActivity("RunScheduler", ActivityKind.Internal);

        // Prime the holiday cache once at the start (eliminates 1460+ individual DB queries)
        _holidayCache = _db.Holidays.ToList();

        var planStartSetting = _db.Settings.FirstOrDefault(s => s.Key == DomainConstants.SettingKeys.PlanStartDate);
        var planStart = DateTime.TryParse(planStartSetting?.Value, out var ps) ? ps : new DateTime(2026, 5, 1);
        var endDate = planStart.AddDays(730);

        var tasks = _db.Tasks.Include(t => t.EffortBreakdown).ToList();
        var resources = _db.Resources.ToList();
        var adjustments = _db.Adjustments.ToList();

        if (!tasks.Any()) return "No tasks to schedule";

        // Build dependency lookup: TaskId -> list of prerequisite TaskIds
        var dependencyMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var task in tasks)
        {
            var deps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(task.DependsOnTaskIds))
            {
                foreach (var dep in task.DependsOnTaskIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    deps.Add(dep);
                }
            }
            dependencyMap[task.TaskId] = deps;
        }

        // Calculate scheduling ranks
        foreach (var task in tasks)
        {
            task.ApplySchedulingRank(CalculateSchedulingRank(task));
        }
        tasks = tasks.OrderBy(t => t.SchedulingRank).ToList();

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

        // Run per-resource allocation
        _db.Allocations.RemoveRange(_db.Allocations);

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

        // Build resource lookup by role
        var resourcesByRole = resources
            .Where(r => r.Active == DomainConstants.ActiveStatus.Yes)
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

        // Build task lookup for dependency checking
        var taskLookup = tasks.ToDictionary(t => t.TaskId, StringComparer.OrdinalIgnoreCase);

        foreach (var calRow in calendar.Where(c => c.IsWorkingDay))
        {
            var calDate = calRow.CalendarDate;
            var calDateKey = calRow.DateKey;

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

                // Check dependency constraints: all prerequisites must be fully completed
                if (dependencyMap.TryGetValue(taskId, out var deps) && deps.Count > 0)
                {
                    var allDepsCompleted = deps.All(depId =>
                    {
                        if (!taskLookup.TryGetValue(depId, out var depTask)) return false;
                        var depBreakdown = depTask.EffortBreakdown.OrderBy(e => e.SortOrder).ToList();
                        return depBreakdown.All(p =>
                            phaseCompletedHours.GetValueOrDefault(depId)?.GetValueOrDefault(p.Role) >= p.EstimationDays * DomainConstants.HoursPerDay);
                    });
                    if (!allDepsCompleted)
                        continue;
                }

                // Parse preferred resources once per task
                var preferredSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(task.PreferredResourceIds))
                {
                    foreach (var pref in task.PreferredResourceIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        preferredSet.Add(pref);
                    }
                }

                // Find the first incomplete phase to work on
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

                    // This is the active phase — allocate resources
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
                            return available >= DomainConstants.MinAllocationHours;
                        }).ToList();
                    }
                    else
                    {
                        eligible = new List<TeamMember>();
                    }

                    // Sort: preferred first, then most available (least loaded)
                    eligible = eligible
                        .OrderByDescending(r => preferredSet.Contains(r.ResourceId) ? 1 : 0)
                        .ThenByDescending(r => perResourceHours[r.ResourceId] - (usedHours.GetValueOrDefault(r.ResourceId)?.GetValueOrDefault(calDate) ?? 0))
                        .ToList();

                    int resourcesAssigned = 0;
                    var maxResources = (int)Math.Ceiling(task.MaxResource);

                    foreach (var resource in eligible)
                    {
                        if (resourcesAssigned >= maxResources) break;
                        if (remainingHours <= 0) break;

                        var used = usedHours.GetValueOrDefault(resource.ResourceId)?.GetValueOrDefault(calDate) ?? 0;
                        var available = perResourceHours[resource.ResourceId] - used;
                        var give = Math.Min(available, remainingHours);
                        give = DomainConstants.MinAllocationHours * Math.Floor(give / DomainConstants.MinAllocationHours);
                        if (give < DomainConstants.MinAllocationHours) continue;

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

                        phaseCompletedHours[taskId][phase.Role] += give;
                        taskResourceIds[taskId].Add(resource.ResourceId);
                        remainingHours -= give;
                        resourcesAssigned++;

                        if (!phaseStartDate[taskId][phase.Role].HasValue)
                            phaseStartDate[taskId][phase.Role] = calDate;

                        if (remainingHours <= 0)
                        {
                            phaseFinishDate[taskId][phase.Role] = calDate;
                            break;
                        }
                    }

                    break; // Only work on first incomplete phase per task per day
                }
            }
        }

        _db.Allocations.AddRange(allocations);

        // Update tasks with scheduling results
        foreach (var task in tasks)
        {
            var taskId = task.TaskId;
            var breakdown = task.EffortBreakdown.OrderBy(e => e.SortOrder).ToList();

            var starts = phaseStartDate.GetValueOrDefault(taskId)?.Values.Where(d => d.HasValue).Select(d => d!.Value).ToList()
                ?? new List<DateTime>();
            var finishes = phaseFinishDate.GetValueOrDefault(taskId)?.Values.Where(d => d.HasValue).Select(d => d!.Value).ToList()
                ?? new List<DateTime>();

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
            var taskAllocs = allocations.Where(a => a.TaskId.Equals(taskId, StringComparison.OrdinalIgnoreCase)).ToList();
            var peakResource = taskAllocs.Count > 0
                ? taskAllocs.GroupBy(a => a.CalendarDate).Max(g => g.Count())
                : 0;

            int duration = 0;
            if (plannedStart.HasValue && plannedFinish.HasValue)
                duration = GetWorkingDaysBetween(plannedStart.Value, plannedFinish.Value);

            string risk = CalculateRisk(plannedFinish, task.StrictDate);

            task.ApplySchedulingResult(
                assignedResource: peakResource,
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
            var dayAllocs = allocations.Where(a => a.CalendarDate.Date == calRow.CalendarDate.Date).ToList();
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

        return resultMessage;
    }

    public Dictionary<string, object> GetDashboardKPIs()
    {
        var tasks = _db.Tasks.Include(t => t.EffortBreakdown).ToList();
        var resources = _db.Resources.Where(r => r.Active == DomainConstants.ActiveStatus.Yes).ToList();

        var totalServices = tasks.Count;
        var totalEstimation = tasks.Sum(t => t.TotalEstimationDays);
        var totalCapacity = resources.Sum(r => r.DailyCapacity * r.AvailabilityPct / 100.0);
        var activeResources = resources.Count;

        var finishDates = tasks.Where(t => t.PlannedFinish.HasValue).Select(t => t.PlannedFinish!.Value).ToList();
        var overallFinish = finishDates.Any() ? finishDates.Max() : (DateTime?)null;

        var strictCount = tasks.Count(t => t.StrictDate.HasValue);
        var onTrack = tasks.Count(t => t.DeliveryRisk == DomainConstants.DeliveryRisk.OnTrack);
        var atRisk = tasks.Count(t => t.DeliveryRisk == DomainConstants.DeliveryRisk.AtRisk);
        var late = tasks.Count(t => t.DeliveryRisk == DomainConstants.DeliveryRisk.Late);

        var assignedResources = tasks.Where(t => t.AssignedResource.HasValue && t.AssignedResource > 0).Select(t => t.AssignedResource!.Value).ToList();
        var avgAssigned = assignedResources.Any() ? assignedResources.Average() : 0;

        var today = _timeProvider.GetLocalNow().LocalDateTime.Date;
        var upcomingStrict = tasks.Where(t => t.StrictDate.HasValue && t.StrictDate >= today)
            .OrderBy(t => t.StrictDate)
            .Take(5)
            .ToList();

        return new Dictionary<string, object>
        {
            ["total_services"] = totalServices,
            ["total_estimation"] = totalEstimation,
            ["total_capacity"] = totalCapacity,
            ["active_resources"] = activeResources,
            ["overall_finish"] = overallFinish ?? DateTime.MinValue,
            ["strict_count"] = strictCount,
            ["on_track"] = onTrack,
            ["at_risk"] = atRisk,
            ["late"] = late,
            ["avg_assigned"] = Math.Round(avgAssigned, 1),
            ["upcoming_strict"] = upcomingStrict
        };
    }

    public List<OutputPlanRowDto> GetOutputPlan()
    {
        var tasks = _db.Tasks.Include(t => t.EffortBreakdown).OrderBy(t => t.SchedulingRank).ToList();
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
}
