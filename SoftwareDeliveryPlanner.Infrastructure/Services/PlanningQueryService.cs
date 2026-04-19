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
            double dayCapacity = dayAllocs.Sum(a => a.AssignedDev);

            var assignedDevs = new List<string>();
            if (dayCapacity > 0)
            {
                int numDevs = (int)Math.Ceiling(dayCapacity);
                for (int i = 0; i < Math.Min(numDevs, resourcesList.Count); i++)
                {
                    assignedDevs.Add(resourcesList[i].ResourceName);
                }
            }

            days.Add(new TaskAssignmentDayDto(
                Date: current,
                DateDisplay: $"{current.Day} {current:ddd}",
                IsWorkingDay: !isWeekend && !isHoliday,
                StatusText: statusText,
                AssignedDevelopers: assignedDevs));

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

        // Load working week settings for correct weekend detection
        var weekSetting = await db.Settings
            .FirstOrDefaultAsync(s => s.Key == DomainConstants.SettingKeys.WorkingWeek, cancellationToken);
        var weekendDays = DomainConstants.WorkingWeek.GetWeekendDays(
            weekSetting?.Value ?? DomainConstants.WorkingWeek.SunThu);

        var adjustments = await db.Adjustments
            .Where(a => a.ResourceId == resourceId)
            .ToListAsync(cancellationToken);

        var tasks = await db.Tasks.ToListAsync(cancellationToken);
        var holidays = await db.Holidays.ToListAsync(cancellationToken);

        var resourcesList = await db.Resources
            .Where(r => r.Active == DomainConstants.ActiveStatus.Yes)
            .OrderBy(r => r.ResourceName)
            .ToListAsync(cancellationToken);

        var devIndex = resourcesList.FindIndex(r => r.ResourceId == resourceId) + 1;

        var days = new List<TimelineDayDto>();
        var current = start;

        while (current <= end)
        {
            var isWeekend = weekendDays.Contains(current.DayOfWeek);
            var isHoliday = holidays.Any(h => h.StartDate.Date <= current.Date && h.EndDate.Date >= current.Date);
            var adjustment = adjustments.FirstOrDefault(
                a => a.AdjStart.Date <= current.Date && a.AdjEnd.Date >= current.Date);

            TaskItem? workingTask = null;
            if (devIndex > 0)
            {
                foreach (var task in tasks)
                {
                    if (task.AssignedDev.HasValue &&
                        Math.Floor(task.AssignedDev.Value) >= devIndex &&
                        task.PlannedStart.HasValue && task.PlannedFinish.HasValue &&
                        current.Date >= task.PlannedStart.Value.Date &&
                        current.Date <= task.PlannedFinish.Value.Date)
                    {
                        workingTask = task;
                        break;
                    }
                }
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
                var weekAllocs = allocations
                    .Where(a => a.CalendarDate >= weekStart && a.CalendarDate < weekEnd)
                    .Sum(a => a.AssignedDev);
                cells.Add(new WorkloadCellDto(resource.ResourceId, resource.ResourceName, weekStart, weekAllocs));
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

    public Task<List<TaskAllocationDto>> GetTaskAllocationsAsync(string taskId, CancellationToken cancellationToken = default)
    {
        // Task allocations are derived from the scheduling engine; return empty for now
        return System.Threading.Tasks.Task.FromResult(new List<TaskAllocationDto>());
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
}
