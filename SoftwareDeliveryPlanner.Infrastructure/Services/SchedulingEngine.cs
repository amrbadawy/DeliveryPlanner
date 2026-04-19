using System.Diagnostics;
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
        int hasStrict = task.StrictDate.HasValue ? 1 : 0;
        long strictVal = task.StrictDate.HasValue ? task.StrictDate.Value.Ticks : long.MaxValue;
        return hasStrict * 10000000 + (int)(strictVal / 10000) + (11 - task.Priority) * 1000;
    }

    private double CalculateEffectiveCapacity(DateTime date, List<TeamMember> resources, List<Adjustment> adjustments)
    {
        double totalCapacity = 0;

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

        var tasks = _db.Tasks.ToList();
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
            var holiday = GetHolidayForDate(current);  // Uses cache, no DB query

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

        // Run allocation
        _db.Allocations.RemoveRange(_db.Allocations);

        var allocations = new List<Allocation>();
        var taskEffort = tasks.ToDictionary(t => t.TaskId, t => 0.0);
        var taskStartDate = tasks.ToDictionary(t => t.TaskId, t => (DateTime?)null);
        var taskPeakDevs = tasks.ToDictionary(t => t.TaskId, t => 0.0);
        var taskAllocations = tasks.ToDictionary(t => t.TaskId, t => new List<Allocation>());
        var allocCounter = 1;

        foreach (var calRow in calendar.Where(c => c.IsWorkingDay))
        {
            var calDate = calRow.CalendarDate;
            var calDateKey = calRow.DateKey;
            var remainingCap = calRow.EffectiveCapacity;

            foreach (var task in tasks)
            {
                var taskId = task.TaskId;
                var maxDev = task.MaxDev;
                var estimation = task.DevEstimation;
                var overrideStart = task.OverrideStart;
                var overrideDev = task.OverrideDev;

                // Check override constraints
                if (overrideStart.HasValue && calDate < overrideStart.Value.Date)
                    continue;

                // Check dependency constraints: all prerequisites must be fully completed
                if (dependencyMap.TryGetValue(taskId, out var deps) && deps.Count > 0)
                {
                    var allDepsCompleted = deps.All(depId =>
                        taskEffort.ContainsKey(depId) &&
                        tasks.Any(t => t.TaskId.Equals(depId, StringComparison.OrdinalIgnoreCase) &&
                                       taskEffort[depId] >= t.DevEstimation));
                    if (!allDepsCompleted)
                        continue;
                }

                var remainingEffort = estimation - taskEffort[taskId];
                if (remainingEffort <= 0) continue;

                double alloc;
                if (overrideDev.HasValue)
                    alloc = Math.Min(overrideDev.Value, Math.Min(maxDev, remainingCap));
                else
                    alloc = Math.Min(maxDev, Math.Min(remainingCap, remainingEffort));

                // Apply minimum increment (0.5)
                if (alloc >= 0.5)
                    alloc = 0.5 * Math.Floor(alloc / 0.5);

                if (alloc < 0.5) continue;

                // Check if higher priority task needs capacity
                var higherRankExists = tasks.Any(t => t.SchedulingRank < task.SchedulingRank && !taskStartDate[t.TaskId].HasValue);
                if (!taskStartDate[taskId].HasValue && higherRankExists && remainingCap >= 0.5)
                    continue;

                // Allocate
                var allocId = $"ALLOC-{allocCounter++:D6}";
                var newAlloc = new Allocation
                {
                    AllocationId = allocId,
                    TaskId = taskId,
                    DateKey = calDateKey,
                    CalendarDate = calDate,
                    SchedRank = task.SchedulingRank,
                    MaxDev = maxDev,
                    AvailableCapacity = remainingCap,
                    AssignedDev = alloc,
                    CumulativeEffort = taskEffort[taskId] + alloc,
                    IsComplete = (taskEffort[taskId] + alloc) >= estimation,
                    ServiceStatus = (taskEffort[taskId] + alloc) >= estimation ? DomainConstants.TaskStatus.Completed : DomainConstants.TaskStatus.InProgress
                };

                if (!taskStartDate[taskId].HasValue)
                    taskStartDate[taskId] = calDate;

                taskEffort[taskId] += alloc;
                taskPeakDevs[taskId] = Math.Max(taskPeakDevs[taskId], alloc);
                taskAllocations[taskId].Add(newAlloc);
                allocations.Add(newAlloc);

                remainingCap -= alloc;
                if (remainingCap < 0.5) break;
            }
        }

        _db.Allocations.AddRange(allocations);

        // Update tasks
        foreach (var task in tasks)
        {
            var taskId = task.TaskId;
            var taskAllocs = taskAllocations[taskId];

            DateTime? plannedStart = taskAllocs.FirstOrDefault()?.CalendarDate;
            DateTime? plannedFinish = taskAllocs.LastOrDefault(a => a.AssignedDev > 0)?.CalendarDate;
            double peakDev = taskPeakDevs[taskId];

            int duration = 0;
            if (plannedStart.HasValue && plannedFinish.HasValue)
                duration = GetWorkingDaysBetween(plannedStart.Value, plannedFinish.Value);

            string status;
            if (!plannedStart.HasValue)
                status = DomainConstants.TaskStatus.NotStarted;
            else if (taskEffort[taskId] >= task.DevEstimation)
                status = DomainConstants.TaskStatus.Completed;
            else
                status = DomainConstants.TaskStatus.InProgress;

            string risk = CalculateRisk(plannedFinish, task.StrictDate);

            task.ApplySchedulingResult(
                assignedDev: peakDev,
                plannedStart: plannedStart,
                plannedFinish: plannedFinish,
                duration: duration,
                status: status,
                deliveryRisk: risk);
        }

        // Update calendar reserved/remaining capacity
        foreach (var calRow in calendar)
        {
            var dayAllocs = allocations.Where(a => a.CalendarDate.Date == calRow.CalendarDate.Date).ToList();
            var reserved = dayAllocs.Sum(a => a.AssignedDev);
            calRow.ReservedCapacity = reserved;
            calRow.RemainingCapacity = calRow.EffectiveCapacity - reserved;

            var existing = _db.Calendar.First(c => c.DateKey == calRow.DateKey);
            existing.ReservedCapacity = reserved;
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
        var tasks = _db.Tasks.ToList();
        var resources = _db.Resources.Where(r => r.Active == DomainConstants.ActiveStatus.Yes).ToList();

        var totalServices = tasks.Count;
        var totalEstimation = tasks.Sum(t => t.DevEstimation);
        var totalCapacity = resources.Sum(r => r.DailyCapacity * r.AvailabilityPct / 100.0);
        var activeResources = resources.Count;

        var finishDates = tasks.Where(t => t.PlannedFinish.HasValue).Select(t => t.PlannedFinish!.Value).ToList();
        var overallFinish = finishDates.Any() ? finishDates.Max() : (DateTime?)null;

        var strictCount = tasks.Count(t => t.StrictDate.HasValue);
        var onTrack = tasks.Count(t => t.DeliveryRisk == DomainConstants.DeliveryRisk.OnTrack);
        var atRisk = tasks.Count(t => t.DeliveryRisk == DomainConstants.DeliveryRisk.AtRisk);
        var late = tasks.Count(t => t.DeliveryRisk == DomainConstants.DeliveryRisk.Late);

        var assignedDevs = tasks.Where(t => t.AssignedDev.HasValue && t.AssignedDev > 0).Select(t => t.AssignedDev!.Value).ToList();
        var avgAssigned = assignedDevs.Any() ? assignedDevs.Average() : 0;

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
        var tasks = _db.Tasks.OrderBy(t => t.SchedulingRank).ToList();
        var output = new List<OutputPlanRowDto>();

        for (int i = 0; i < tasks.Count; i++)
        {
            var task = tasks[i];
            output.Add(new OutputPlanRowDto(
                Num: i + 1,
                TaskId: task.TaskId,
                ServiceName: task.ServiceName,
                AssignedDev: task.AssignedDev,
                PlannedStart: task.PlannedStart?.ToString("yyyy-MM-dd"),
                PlannedFinish: task.PlannedFinish?.ToString("yyyy-MM-dd"),
                Duration: task.Duration,
                DevEstimation: task.DevEstimation,
                StrictDate: task.StrictDate?.ToString("yyyy-MM-dd"),
                Status: task.Status,
                DeliveryRisk: task.DeliveryRisk));
        }

        return output;
    }

    public void Dispose()
    {
        if (_ownsContext)
            _db.Dispose();
    }
}
