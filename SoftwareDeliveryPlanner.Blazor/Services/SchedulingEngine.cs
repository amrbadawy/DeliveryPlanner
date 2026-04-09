using SoftwareDeliveryPlanner.Data;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Services;

public class SchedulingEngine
{
    private readonly PlannerDbContext _db;
    private readonly int _atRiskThreshold;

    public SchedulingEngine(PlannerDbContext db)
    {
        _db = db;
        var setting = _db.Settings.FirstOrDefault(s => s.Key == "at_risk_threshold");
        _atRiskThreshold = int.TryParse(setting?.Value, out var t) ? t : 5;
    }

    public bool IsWorkingDay(DateTime date)
    {
        // Working days: Sunday(0), Monday(1), Tuesday(2), Wednesday(3), Thursday(4)
        // Non-working: Friday(5), Saturday(6)
        if ((int)date.DayOfWeek == 5 || (int)date.DayOfWeek == 6)
            return false;

        // Check holidays
        var isHoliday = _db.Holidays.Any(h => h.HolidayDate.Date == date.Date);
        return !isHoliday;
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
            if (resource.Active != "Yes") continue;

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
        if (!strictDate.HasValue) return "On Track";
        if (!plannedFinish.HasValue) return "At Risk";

        if (plannedFinish.Value > strictDate.Value) return "Late";

        var workingDaysLeft = GetWorkingDaysBetween(DateTime.Today, strictDate.Value);
        if (workingDaysLeft <= _atRiskThreshold) return "At Risk";

        return "On Track";
    }

    public string RunScheduler()
    {
        var planStartSetting = _db.Settings.FirstOrDefault(s => s.Key == "plan_start_date");
        var planStart = DateTime.TryParse(planStartSetting?.Value, out var ps) ? ps : new DateTime(2026, 5, 1);
        var endDate = planStart.AddDays(730);

        var tasks = _db.Tasks.ToList();
        var resources = _db.Resources.ToList();
        var adjustments = _db.Adjustments.ToList();

        if (!tasks.Any()) return "No tasks to schedule";

        // Calculate scheduling ranks
        foreach (var task in tasks)
        {
            task.SchedulingRank = CalculateSchedulingRank(task);
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
            var holiday = _db.Holidays.FirstOrDefault(h => h.HolidayDate.Date == current.Date);
            
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
                    DateKey = dateKey,
                    CalendarDate = calDate,
                    SchedRank = task.SchedulingRank,
                    MaxDev = maxDev,
                    AvailableCapacity = remainingCap,
                    AssignedDev = alloc,
                    CumulativeEffort = taskEffort[taskId] + alloc,
                    IsComplete = (taskEffort[taskId] + alloc) >= estimation,
                    ServiceStatus = (taskEffort[taskId] + alloc) >= estimation ? "Completed" : "In Progress"
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
                status = "Not Started";
            else if (taskEffort[taskId] >= task.DevEstimation)
                status = "Completed";
            else
                status = "In Progress";

            string risk = CalculateRisk(plannedFinish, task.StrictDate);

            task.AssignedDev = peakDev;
            task.PlannedStart = plannedStart;
            task.PlannedFinish = plannedFinish;
            task.Duration = duration;
            task.Status = status;
            task.DeliveryRisk = risk;
            task.UpdatedAt = DateTime.Now;
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

        return $"Successfully scheduled {tasks.Count} tasks with {allocations.Count} allocations";
    }

    public Dictionary<string, object> GetDashboardKPIs()
    {
        var tasks = _db.Tasks.ToList();
        var resources = _db.Resources.Where(r => r.Active == "Yes").ToList();

        var totalServices = tasks.Count;
        var totalEstimation = tasks.Sum(t => t.DevEstimation);
        var totalCapacity = resources.Sum(r => r.DailyCapacity * r.AvailabilityPct / 100.0);
        var activeResources = resources.Count;

        var finishDates = tasks.Where(t => t.PlannedFinish.HasValue).Select(t => t.PlannedFinish!.Value).ToList();
        var overallFinish = finishDates.Any() ? finishDates.Max() : (DateTime?)null;

        var strictCount = tasks.Count(t => t.StrictDate.HasValue);
        var onTrack = tasks.Count(t => t.DeliveryRisk == "On Track");
        var atRisk = tasks.Count(t => t.DeliveryRisk == "At Risk");
        var late = tasks.Count(t => t.DeliveryRisk == "Late");

        var assignedDevs = tasks.Where(t => t.AssignedDev.HasValue && t.AssignedDev > 0).Select(t => t.AssignedDev!.Value).ToList();
        var avgAssigned = assignedDevs.Any() ? assignedDevs.Average() : 0;

        var upcomingStrict = tasks.Where(t => t.StrictDate.HasValue && t.StrictDate >= DateTime.Today)
            .OrderBy(t => t.StrictDate)
            .Take(5)
            .ToList();

        return new Dictionary<string, object>
        {
            ["total_services"] = totalServices,
            ["total_estimation"] = totalEstimation,
            ["total_capacity"] = totalCapacity,
            ["active_resources"] = activeResources,
            ["overall_finish"] = overallFinish,
            ["strict_count"] = strictCount,
            ["on_track"] = onTrack,
            ["at_risk"] = atRisk,
            ["late"] = late,
            ["avg_assigned"] = Math.Round(avgAssigned, 1),
            ["upcoming_strict"] = upcomingStrict
        };
    }

    public List<Dictionary<string, object?>> GetOutputPlan()
    {
        var tasks = _db.Tasks.OrderBy(t => t.SchedulingRank).ToList();
        var output = new List<Dictionary<string, object?>>();

        for (int i = 0; i < tasks.Count; i++)
        {
            var task = tasks[i];
            output.Add(new Dictionary<string, object?>
            {
                ["num"] = i + 1,
                ["task_id"] = task.TaskId,
                ["service_name"] = task.ServiceName,
                ["assigned_dev"] = task.AssignedDev,
                ["planned_start"] = task.PlannedStart?.ToString("yyyy-MM-dd"),
                ["planned_finish"] = task.PlannedFinish?.ToString("yyyy-MM-dd"),
                ["duration"] = task.Duration,
                ["dev_estimation"] = task.DevEstimation,
                ["strict_date"] = task.StrictDate?.ToString("yyyy-MM-dd"),
                ["status"] = task.Status,
                ["delivery_risk"] = task.DeliveryRisk
            });
        }

        return output;
    }
}
