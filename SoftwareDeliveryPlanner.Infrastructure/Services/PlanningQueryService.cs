using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Application.Planning.Queries;
using SoftwareDeliveryPlanner.Application.Tasks.Queries;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Data;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal sealed class PlanningQueryService : ServiceBase, IPlanningQueryService
{
    public PlanningQueryService(
        IDbContextFactory<PlannerDbContext> dbFactory,
        IDbContextFactory<ReadOnlyPlannerDbContext> readOnlyDbFactory,
        ISchedulingEngineFactory engineFactory,
        IPublisher publisher)
        : base(dbFactory, readOnlyDbFactory, engineFactory, publisher) { }

    public async Task<List<CalendarDay>> GetCalendarAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Calendar.OrderBy(c => c.CalendarDate).ToListAsync(cancellationToken);
    }

    public async Task<List<OutputPlanRowDto>> GetOutputPlanAsync(CancellationToken cancellationToken = default)
    {
        using var engine = await EngineFactory.CreateAsync(cancellationToken);
        return engine.GetOutputPlan();
    }

    public async Task<TaskTimelineDto> GetTaskTimelineAsync(string taskId, CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.TaskId == taskId, cancellationToken);
        if (task == null || !task.PlannedStart.HasValue || !task.PlannedFinish.HasValue)
        {
            return new TaskTimelineDto(new List<TaskAssignmentDayDto>());
        }

        var weekSetting = await db.Settings
            .FirstOrDefaultAsync(s => s.Key == DomainConstants.SettingKeys.WorkingWeek, cancellationToken);
        var weekendDays = DomainConstants.WorkingWeek.GetWeekendDays(
            weekSetting?.Value ?? DomainConstants.WorkingWeek.SunThu);

        var holidays = await db.Holidays.ToListAsync(cancellationToken);

        var resourcesList = await db.Resources
            .Where(r => r.Active == DomainConstants.ActiveStatus.Yes)
            .OrderBy(r => r.ResourceName)
            .ToListAsync(cancellationToken);

        var allocations = await db.Allocations
            .Where(a => a.TaskId == taskId)
            .ToListAsync(cancellationToken);

        var days = new List<TaskAssignmentDayDto>();
        var current = task.PlannedStart.Value;
        var end = task.PlannedFinish.Value;

        while (current <= end)
        {
            var isWeekend = weekendDays.Contains(current.DayOfWeek);
            var isHoliday = holidays.Any(h => h.StartDate.Date <= current.Date && h.EndDate.Date >= current.Date);

            string statusText = "Working";
            if (isWeekend) statusText = "Weekend";
            else if (isHoliday) statusText = "Holiday";

            var dayAllocs = allocations.Where(a => a.CalendarDate.Date == current.Date).ToList();
            var assignedResources = dayAllocs
                .GroupBy(a => a.ResourceId)
                .Select(g =>
                {
                    var res = resourcesList.FirstOrDefault(r => r.ResourceId == g.Key);
                    return new AssignedResourceInfo(g.Key, res?.ResourceName ?? g.Key, g.First().Role);
                }).ToList();

            days.Add(new TaskAssignmentDayDto(
                Date: current,
                DateDisplay: current.ToString("dd MMMM yyyy"),
                IsWorkingDay: !isWeekend && !isHoliday,
                StatusText: statusText,
                AssignedResources: assignedResources));

            current = current.AddDays(1);
        }

        return new TaskTimelineDto(days);
    }

    public async Task<TimelineDataDto> GetTimelineDataAsync(
        string resourceId,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);

        var weekSetting = await db.Settings
            .FirstOrDefaultAsync(s => s.Key == DomainConstants.SettingKeys.WorkingWeek, cancellationToken);
        var weekendDays = DomainConstants.WorkingWeek.GetWeekendDays(
            weekSetting?.Value ?? DomainConstants.WorkingWeek.SunThu);

        var adjustments = await db.Adjustments
            .Where(a => a.ResourceId == resourceId)
            .ToListAsync(cancellationToken);

        var tasks = await db.Tasks.ToListAsync(cancellationToken);
        var holidays = await db.Holidays.ToListAsync(cancellationToken);

        var resourceAllocations = await db.Allocations
            .Where(a => a.ResourceId == resourceId)
            .ToListAsync(cancellationToken);

        var days = new List<TimelineDayDto>();
        var current = start;

        while (current <= end)
        {
            var isWeekend = weekendDays.Contains(current.DayOfWeek);
            var isHoliday = holidays.Any(h => h.StartDate.Date <= current.Date && h.EndDate.Date >= current.Date);
            var adjustment = adjustments.FirstOrDefault(
                a => a.AdjStart.Date <= current.Date && a.AdjEnd.Date >= current.Date);

            TaskItem? workingTask = null;
            var dayAlloc = resourceAllocations.FirstOrDefault(a => a.CalendarDate.Date == current.Date);
            if (dayAlloc != null)
            {
                workingTask = tasks.FirstOrDefault(t => t.TaskId == dayAlloc.TaskId);
            }

            TimelineDayStatus status;
            string statusText;

            if (isWeekend)
            {
                status = TimelineDayStatus.Weekend;
                statusText = current.DayOfWeek.ToString();
            }
            else if (isHoliday)
            {
                status = TimelineDayStatus.Holiday;
                var h = holidays.First(x => x.StartDate.Date <= current.Date && x.EndDate.Date >= current.Date);
                statusText = h.HolidayName;
            }
            else if (adjustment != null)
            {
                status = TimelineDayStatus.Adjustment;
                statusText = adjustment.AdjType;
            }
            else if (workingTask != null)
            {
                status = TimelineDayStatus.Working;
                statusText = workingTask.ServiceName;
            }
            else
            {
                status = TimelineDayStatus.Free;
                statusText = "Free";
            }

            days.Add(new TimelineDayDto(
                Date: current,
                DateDisplay: $"{current.Day} {current:ddd}",
                Status: status,
                StatusText: statusText,
                TaskId: workingTask?.TaskId,
                TaskName: workingTask?.ServiceName));

            current = current.AddDays(1);
        }

        return new TimelineDataDto(days);
    }

    public async Task<WorkloadHeatmapDto> GetWorkloadHeatmapAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);

        var resources = await db.Resources
            .Where(r => r.Active == DomainConstants.ActiveStatus.Yes)
            .OrderBy(r => r.ResourceName)
            .ToListAsync(cancellationToken);

        var allocations = await db.Allocations.ToListAsync(cancellationToken);

        var resourceNames = resources.Select(r => r.ResourceName).ToList();

        // Group allocations by week
        var weekStarts = allocations
            .Select(a => a.CalendarDate.Date.AddDays(-(int)a.CalendarDate.DayOfWeek))
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var cells = new List<WorkloadCellDto>();
        foreach (var resource in resources)
        {
            foreach (var weekStart in weekStarts)
            {
                var weekEnd = weekStart.AddDays(7);
                var weekHours = allocations
                    .Where(a => a.ResourceId == resource.ResourceId && a.CalendarDate >= weekStart && a.CalendarDate < weekEnd)
                    .Sum(a => a.HoursAllocated);
                var maxWeeklyHours = 5 * resource.DailyCapacity * (resource.AvailabilityPct / 100.0) * DomainConstants.HoursPerDay;
                var utilization = maxWeeklyHours > 0 ? (weekHours / maxWeeklyHours) * 100 : 0;
                cells.Add(new WorkloadCellDto(resource.ResourceId, resource.ResourceName, weekStart, utilization));
            }
        }

        return new WorkloadHeatmapDto(resourceNames, weekStarts, cells);
    }

    public async Task<List<RiskTrendPointDto>> GetRiskTrendAsync(int maxPoints)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync();

        var snapshots = await db.SchedulerSnapshots
            .OrderByDescending(s => s.RunTimestamp)
            .Take(maxPoints)
            .ToListAsync();

        return snapshots
            .OrderBy(s => s.RunTimestamp)
            .Select(s => new RiskTrendPointDto(s.RunTimestamp, s.OnTrackCount, s.AtRiskCount, s.LateCount, s.TotalTasks))
            .ToList();
    }

    public async Task<List<TaskAllocationDto>> GetTaskAllocationsAsync(string taskId, CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);

        var allocations = await db.Allocations
            .Where(a => a.TaskId == taskId)
            .ToListAsync(cancellationToken);

        if (!allocations.Any())
            return new List<TaskAllocationDto>();

        var resourcesList = await db.Resources
            .Where(r => r.Active == DomainConstants.ActiveStatus.Yes)
            .OrderBy(r => r.ResourceName)
            .ToListAsync(cancellationToken);

        return allocations
            .GroupBy(a => a.ResourceId)
            .Select(g =>
            {
                var res = resourcesList.FirstOrDefault(r => r.ResourceId == g.Key);
                return new TaskAllocationDto(
                    ResourceId: g.Key,
                    ResourceName: res?.ResourceName ?? g.Key,
                    Role: g.First().Role,
                    Team: res?.Team ?? "",
                    AvailabilityPct: res?.AvailabilityPct ?? 0,
                    HoursAllocated: g.Sum(a => a.HoursAllocated),
                    StartDate: g.Min(a => a.CalendarDate),
                    EndDate: g.Max(a => a.CalendarDate));
            }).ToList();
    }

    public async Task<DateTime?> GetLastSchedulerRunAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        var setting = await db.Settings
            .FirstOrDefaultAsync(s => s.Key == DomainConstants.SettingKeys.LastSchedulerRun, cancellationToken);

        if (setting?.Value is not null && DateTime.TryParse(setting.Value, out var lastRun))
            return lastRun;

        return null;
    }

    public async Task<List<ResourceUtilizationDto>> GetResourceUtilizationAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);

        var resources = await db.Resources
            .Where(r => r.Active == DomainConstants.ActiveStatus.Yes)
            .OrderBy(r => r.ResourceName)
            .ToListAsync(cancellationToken);
        var allocations = await db.Allocations.ToListAsync(cancellationToken);
        var calendar = await db.Calendar.Where(c => c.IsWorkingDay).ToListAsync(cancellationToken);
        var adjustments = await db.Adjustments.ToListAsync(cancellationToken);

        var result = new List<ResourceUtilizationDto>();
        foreach (var resource in resources)
        {
            var workingDays = calendar.Where(c =>
                c.CalendarDate >= resource.StartDate &&
                (!resource.EndDate.HasValue || c.CalendarDate <= resource.EndDate.Value)).ToList();

            double totalAvailable = 0;
            int benchDays = 0;
            int overallocatedDays = 0;

            foreach (var day in workingDays)
            {
                double capacity = resource.DailyCapacity * (resource.AvailabilityPct / 100.0) * DomainConstants.HoursPerDay;
                var adj = adjustments.FirstOrDefault(a =>
                    a.ResourceId == resource.ResourceId &&
                    a.AdjStart <= day.CalendarDate &&
                    a.AdjEnd >= day.CalendarDate);
                if (adj != null) capacity *= adj.AvailabilityPct / 100.0;

                totalAvailable += capacity;

                var dayHours = allocations
                    .Where(a => a.ResourceId == resource.ResourceId && a.CalendarDate.Date == day.CalendarDate.Date)
                    .Sum(a => a.HoursAllocated);
                if (dayHours == 0) benchDays++;
                if (dayHours > capacity + 0.01) overallocatedDays++;
            }

            var totalAllocated = allocations
                .Where(a => a.ResourceId == resource.ResourceId)
                .Sum(a => a.HoursAllocated);
            var utilPct = totalAvailable > 0 ? (totalAllocated / totalAvailable) * 100 : 0;

            result.Add(new ResourceUtilizationDto(
                resource.ResourceId, resource.ResourceName, resource.Role,
                Math.Round(totalAvailable, 1), Math.Round(totalAllocated, 1),
                Math.Round(utilPct, 1), benchDays, overallocatedDays));
        }

        return result;
    }

    public async Task<List<OverallocationAlertDto>> GetOverallocationAlertsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);

        var resources = await db.Resources
            .Where(r => r.Active == DomainConstants.ActiveStatus.Yes)
            .ToListAsync(cancellationToken);
        var allocations = await db.Allocations.ToListAsync(cancellationToken);
        var adjustments = await db.Adjustments.ToListAsync(cancellationToken);
        var weekSetting = await db.Settings
            .FirstOrDefaultAsync(s => s.Key == DomainConstants.SettingKeys.WorkingWeek, cancellationToken);
        var globalWeekendDays = DomainConstants.WorkingWeek.GetWeekendDays(
            weekSetting?.Value ?? DomainConstants.WorkingWeek.SunThu);
        var holidays = await db.Holidays.ToListAsync(cancellationToken);

        var alerts = new List<OverallocationAlertDto>();

        // Group allocations by resource+date
        var grouped = allocations.GroupBy(a => new { a.ResourceId, Date = a.CalendarDate.Date });

        foreach (var group in grouped)
        {
            var resource = resources.FirstOrDefault(r => r.ResourceId.Equals(group.Key.ResourceId, StringComparison.OrdinalIgnoreCase));
            if (resource == null) continue;

            var date = group.Key.Date;
            var weekendDays = resource.WorkingWeek != null
                ? DomainConstants.WorkingWeek.GetWeekendDays(resource.WorkingWeek)
                : globalWeekendDays;
            if (weekendDays.Contains(date.DayOfWeek)) continue;
            if (holidays.Any(h => h.StartDate.Date <= date && h.EndDate.Date >= date)) continue;

            double capacity = resource.DailyCapacity * (resource.AvailabilityPct / 100.0) * DomainConstants.HoursPerDay;
            var adj = adjustments.FirstOrDefault(a =>
                a.ResourceId == resource.ResourceId && a.AdjStart <= date && a.AdjEnd >= date);
            if (adj != null) capacity *= adj.AvailabilityPct / 100.0;

            var totalHours = group.Sum(a => a.HoursAllocated);
            if (totalHours > capacity + 0.01)
            {
                var overage = totalHours - capacity;
                var taskIds = group.Select(a => a.TaskId).Distinct().OrderBy(id => id).ToList();
                var severity = overage >= 2 * DomainConstants.HoursPerDay ? "Critical" : "Warning";

                alerts.Add(new OverallocationAlertDto(
                    resource.ResourceId, resource.ResourceName, date,
                    Math.Round(totalHours, 1), Math.Round(capacity, 1), Math.Round(overage, 1),
                    taskIds, severity));
            }
        }

        return alerts.OrderByDescending(a => a.Severity).ThenBy(a => a.Date).ToList();
    }

    public async Task<UtilizationForecastDto> GetUtilizationForecastAsync(int weeksAhead = 26, CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);

        var resources = await db.Resources
            .Where(r => r.Active == DomainConstants.ActiveStatus.Yes)
            .ToListAsync(cancellationToken);
        var allocations = await db.Allocations.ToListAsync(cancellationToken);
        var adjustments = await db.Adjustments.ToListAsync(cancellationToken);
        var weekSetting = await db.Settings
            .FirstOrDefaultAsync(s => s.Key == DomainConstants.SettingKeys.WorkingWeek, cancellationToken);
        var globalWeekendDays = DomainConstants.WorkingWeek.GetWeekendDays(
            weekSetting?.Value ?? DomainConstants.WorkingWeek.SunThu);
        var holidays = await db.Holidays.ToListAsync(cancellationToken);

        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek); // Sunday
        var weeks = new List<ForecastWeekDto>();

        for (int w = 0; w < weeksAhead; w++)
        {
            var ws = weekStart.AddDays(w * 7);
            var we = ws.AddDays(7);

            double totalCapacity = 0;
            double totalAllocated = 0;
            int overallocatedResources = 0;

            foreach (var resource in resources)
            {
                var resWeekendDays = resource.WorkingWeek != null
                    ? DomainConstants.WorkingWeek.GetWeekendDays(resource.WorkingWeek)
                    : globalWeekendDays;

                double weekCapacity = 0;
                double weekAllocated = 0;
                bool anyOverallocated = false;

                for (var d = ws; d < we; d = d.AddDays(1))
                {
                    if (resWeekendDays.Contains(d.DayOfWeek)) continue;
                    if (holidays.Any(h => h.StartDate.Date <= d && h.EndDate.Date >= d)) continue;
                    if (d < resource.StartDate) continue;
                    if (resource.EndDate.HasValue && d > resource.EndDate.Value) continue;

                    double dayCap = resource.DailyCapacity * (resource.AvailabilityPct / 100.0) * DomainConstants.HoursPerDay;
                    var adj = adjustments.FirstOrDefault(a =>
                        a.ResourceId == resource.ResourceId && a.AdjStart <= d && a.AdjEnd >= d);
                    if (adj != null) dayCap *= adj.AvailabilityPct / 100.0;

                    weekCapacity += dayCap;

                    var dayAlloc = allocations
                        .Where(a => a.ResourceId == resource.ResourceId && a.CalendarDate.Date == d)
                        .Sum(a => a.HoursAllocated);
                    weekAllocated += dayAlloc;
                    if (dayAlloc > dayCap + 0.01) anyOverallocated = true;
                }

                totalCapacity += weekCapacity;
                totalAllocated += weekAllocated;
                if (anyOverallocated) overallocatedResources++;
            }

            var utilPct = totalCapacity > 0 ? (totalAllocated / totalCapacity) * 100 : 0;
            weeks.Add(new ForecastWeekDto(ws,
                Math.Round(totalCapacity, 1), Math.Round(totalAllocated, 1),
                Math.Round(utilPct, 1), Math.Round(totalCapacity - totalAllocated, 1),
                overallocatedResources));
        }

        return new UtilizationForecastDto(weeks);
    }

    public async Task<List<FeasibilityResultDto>> GetFeasibilityAsync(string? taskId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);

        var tasks = await db.Tasks
            .Include(t => t.EffortBreakdown)
            .ToListAsync(cancellationToken);

        var targetTasks = tasks
            .Where(t => t.StrictDate.HasValue)
            .Where(t => taskId == null || t.TaskId.Equals(taskId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!targetTasks.Any())
            return new List<FeasibilityResultDto>();

        var resources = await db.Resources
            .Where(r => r.Active == DomainConstants.ActiveStatus.Yes)
            .ToListAsync(cancellationToken);
        var adjustments = await db.Adjustments.ToListAsync(cancellationToken);
        var holidays = await db.Holidays.ToListAsync(cancellationToken);
        var weekSetting = await db.Settings
            .FirstOrDefaultAsync(s => s.Key == DomainConstants.SettingKeys.WorkingWeek, cancellationToken);
        var globalWeekendDays = DomainConstants.WorkingWeek.GetWeekendDays(
            weekSetting?.Value ?? DomainConstants.WorkingWeek.SunThu);
        var planStartSetting = await db.Settings
            .FirstOrDefaultAsync(s => s.Key == DomainConstants.SettingKeys.PlanStartDate, cancellationToken);
        var planStart = DateTime.TryParse(planStartSetting?.Value, out var ps) ? ps : new DateTime(2026, 5, 1);
        var today = DateTime.Today;
        var simStart = planStart > today ? planStart : today;

        var results = new List<FeasibilityResultDto>();

        foreach (var task in targetTasks)
        {
            var phases = task.EffortBreakdown.OrderBy(e => e.SortOrder).ToList();
            if (phases.Count == 0)
            {
                results.Add(new FeasibilityResultDto(task.TaskId, task.ServiceName, true, null, 0, null, null));
                continue;
            }

            // Best-case: simulate sequential phases with all eligible resources available
            double totalHours = phases.Sum(p => p.EstimationDays * DomainConstants.HoursPerDay);
            var eligibleByRole = resources.GroupBy(r => r.Role, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            // Estimate earliest finish by computing working days needed per phase
            double remainingHours = totalHours;
            int workingDaysNeeded = 0;
            foreach (var phase in phases)
            {
                var phaseHours = phase.EstimationDays * DomainConstants.HoursPerDay;
                if (eligibleByRole.TryGetValue(phase.Role, out var roleRes) && roleRes.Count > 0)
                {
                    var maxDailyHours = Math.Min((int)Math.Ceiling(phase.MaxFte), roleRes.Count) * DomainConstants.HoursPerDay;
                    var phaseDays = (int)Math.Ceiling(phaseHours / maxDailyHours);
                    // Account for overlap with previous phase
                    var overlapReduction = phase.OverlapPct > 0 ? (int)(phaseDays * phase.OverlapPct / 100.0) : 0;
                    workingDaysNeeded += Math.Max(1, phaseDays - overlapReduction);
                }
                else
                {
                    workingDaysNeeded += (int)Math.Ceiling(phase.EstimationDays);
                }
            }

            // Find the earliest finish date by walking working days
            var current = simStart;
            int counted = 0;
            while (counted < workingDaysNeeded)
            {
                if (!globalWeekendDays.Contains(current.DayOfWeek) &&
                    !holidays.Any(h => h.StartDate.Date <= current && h.EndDate.Date >= current))
                    counted++;
                if (counted < workingDaysNeeded)
                    current = current.AddDays(1);
            }
            var earliestFinish = current;

            var slackDays = 0;
            if (task.StrictDate.HasValue)
            {
                var s = earliestFinish;
                while (s < task.StrictDate.Value)
                {
                    s = s.AddDays(1);
                    if (!globalWeekendDays.Contains(s.DayOfWeek) &&
                        !holidays.Any(h => h.StartDate.Date <= s && h.EndDate.Date >= s))
                        slackDays++;
                }
            }

            var isFeasible = !task.StrictDate.HasValue || earliestFinish <= task.StrictDate.Value;
            string? bottleneck = null;
            string? recommendation = null;

            if (!isFeasible)
            {
                // Find bottleneck role
                var worstRole = phases
                    .Where(p => !eligibleByRole.ContainsKey(p.Role) || eligibleByRole[p.Role].Count == 0)
                    .Select(p => p.Role)
                    .FirstOrDefault();
                if (worstRole != null)
                {
                    bottleneck = $"No {worstRole} resources available";
                    recommendation = $"Add at least 1 {worstRole} resource or extend deadline";
                }
                else
                {
                    var longestPhase = phases.OrderByDescending(p => p.EstimationDays).First();
                    bottleneck = $"{longestPhase.Role} phase requires {longestPhase.EstimationDays} days";
                    var daysOver = workingDaysNeeded - slackDays;
                    recommendation = $"Add resources for {longestPhase.Role} or extend deadline by {daysOver} working days";
                }
            }

            results.Add(new FeasibilityResultDto(
                task.TaskId, task.ServiceName, isFeasible, earliestFinish,
                isFeasible ? slackDays : -slackDays, bottleneck, recommendation));
        }

        return results;
    }

    public async Task<List<TaskGanttSegmentsDto>> GetGanttSegmentsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);

        // Load all allocations grouped by (TaskId, Role) to derive segment date windows
        var allocations = await db.Allocations
            .Where(a => a.HoursAllocated > 0)
            .OrderBy(a => a.CalendarDate)
            .ToListAsync(cancellationToken);

        // Load tasks with effort breakdown for MaxFte
        var tasks = await db.Tasks
            .Include(t => t.EffortBreakdown)
            .ToListAsync(cancellationToken);

        var effortByTask = tasks.ToDictionary(
            t => t.TaskId,
            t => t.EffortBreakdown.ToDictionary(e => e.Role, e => e.MaxFte, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        // Load resources for names
        var resources = await db.Resources.ToListAsync(cancellationToken);
        var resourceNames = resources.ToDictionary(r => r.ResourceId, r => r.ResourceName, StringComparer.OrdinalIgnoreCase);

        var grouped = allocations
            .GroupBy(a => a.TaskId, StringComparer.OrdinalIgnoreCase)
            .Select(taskGroup =>
            {
                var segments = taskGroup
                    .GroupBy(a => a.Role, StringComparer.OrdinalIgnoreCase)
                    .Select(roleGroup =>
                    {
                        var segStart = roleGroup.Min(a => a.CalendarDate);
                        var segEnd = roleGroup.Max(a => a.CalendarDate);
                        var duration = (int)(segEnd - segStart).TotalDays + 1;
                        var maxFte = effortByTask.GetValueOrDefault(taskGroup.Key)
                            ?.GetValueOrDefault(roleGroup.Key) ?? 1.0;

                        var assignedResources = roleGroup
                            .Select(a => a.ResourceId)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .Select(rid => new GanttSegmentResourceDto(
                                rid,
                                resourceNames.GetValueOrDefault(rid) ?? rid))
                            .ToList();

                        return new GanttRoleSegmentDto(
                            taskGroup.Key,
                            roleGroup.Key,
                            segStart,
                            segEnd,
                            duration,
                            maxFte,
                            assignedResources);
                    })
                    .OrderBy(s => s.SegmentStart)
                    .ThenBy(s => s.Role)
                    .ToList();

                return new TaskGanttSegmentsDto(taskGroup.Key, segments);
            })
            .ToList();

        return grouped;
    }
}
