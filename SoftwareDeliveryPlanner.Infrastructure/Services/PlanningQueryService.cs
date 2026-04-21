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
                DateDisplay: $"{current.Day} {current:ddd}",
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
}
