using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Infrastructure.Data;
using SoftwareDeliveryPlanner.Infrastructure.Extensions;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal sealed class SchedulingOrchestrator : ISchedulingOrchestrator
{
    private readonly IDbContextFactory<PlannerDbContext> _dbFactory;
    private readonly IDbContextFactory<ReadOnlyPlannerDbContext> _readOnlyDbFactory;
    private readonly TimeProvider _timeProvider;
    private readonly IPublisher _publisher;

    public SchedulingOrchestrator(
        IDbContextFactory<PlannerDbContext> dbFactory,
        IDbContextFactory<ReadOnlyPlannerDbContext> readOnlyDbFactory,
        TimeProvider timeProvider,
        IPublisher publisher)
    {
        _dbFactory = dbFactory;
        _readOnlyDbFactory = readOnlyDbFactory;
        _timeProvider = timeProvider;
        _publisher = publisher;
    }

    // ─────────────────────────────────────────────────────────
    // Scheduler
    // ─────────────────────────────────────────────────────────

    public async Task<string> RunSchedulerAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var scheduler = new SchedulingEngine(db, _timeProvider);
        return scheduler.RunScheduler();
    }

    // ─────────────────────────────────────────────────────────
    // Dashboard KPIs
    // ─────────────────────────────────────────────────────────

    public async Task<DashboardKpisDto> GetDashboardKpisAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var scheduler = new SchedulingEngine(db, _timeProvider);
        var kpis = scheduler.GetDashboardKPIs();

        var rawFinish = kpis["overall_finish"] as DateTime?;
        var overallFinish = rawFinish.HasValue && rawFinish.Value > DateTime.MinValue ? rawFinish : null;

        return new DashboardKpisDto(
            TotalServices: (int)kpis["total_services"],
            TotalEstimation: (double)kpis["total_estimation"],
            ActiveResources: (int)kpis["active_resources"],
            TotalCapacity: (double)kpis["total_capacity"],
            OverallFinish: overallFinish,
            OnTrack: (int)kpis["on_track"],
            AtRisk: (int)kpis["at_risk"],
            Late: (int)kpis["late"],
            AvgAssigned: (double)kpis["avg_assigned"]);
    }

    // ─────────────────────────────────────────────────────────
    // Tasks
    // ─────────────────────────────────────────────────────────

    public async Task<List<TaskItem>> GetTasksAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _readOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Tasks.OrderBy(t => t.SchedulingRank ?? 999).ToListAsync(cancellationToken);
    }

    public async Task<int> GetTaskCountAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _readOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Tasks.CountAsync(cancellationToken);
    }

    public async Task UpsertTaskAsync(
        int id, string taskId, string serviceName, double devEstimation,
        double maxDev, int priority, DateTime? strictDate,
        string? dependsOnTaskIds, bool isNew,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        if (isNew)
        {
            var task = TaskItem.Create(taskId, serviceName, devEstimation, maxDev, priority, strictDate, dependsOnTaskIds);
            db.Tasks.Add(task);
        }
        else
        {
            var existing = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
            existing?.Update(serviceName, devEstimation, maxDev, priority, strictDate, dependsOnTaskIds);
        }

        await db.SaveChangesAsync(cancellationToken);
        await db.DispatchDomainEventsAsync(_publisher, cancellationToken);

        await using var schedulerDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
        new SchedulingEngine(schedulerDb, _timeProvider).RunScheduler();
    }

    public async Task DeleteTaskAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task != null)
        {
            db.Tasks.Remove(task);
            await db.SaveChangesAsync(cancellationToken);
        }

        await using var schedulerDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
        new SchedulingEngine(schedulerDb, _timeProvider).RunScheduler();
    }

    // ─────────────────────────────────────────────────────────
    // Resources
    // ─────────────────────────────────────────────────────────

    public async Task<List<TeamMember>> GetResourcesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _readOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Resources.ToListAsync(cancellationToken);
    }

    public async Task<int> GetResourceCountAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _readOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Resources.CountAsync(cancellationToken);
    }

    public async Task UpsertResourceAsync(
        int id, string resourceId, string resourceName, string role,
        string team, double availabilityPct, double dailyCapacity,
        DateTime startDate, string active, string? notes, bool isNew,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        if (isNew)
        {
            var resource = TeamMember.Create(resourceId, resourceName, role, team, availabilityPct, dailyCapacity, startDate, active: active, notes: notes);
            db.Resources.Add(resource);
        }
        else
        {
            var existing = await db.Resources.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            existing?.Update(resourceName, role, team, availabilityPct, dailyCapacity, startDate, active, notes);
        }

        await db.SaveChangesAsync(cancellationToken);
        await db.DispatchDomainEventsAsync(_publisher, cancellationToken);

        await using var schedulerDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
        new SchedulingEngine(schedulerDb, _timeProvider).RunScheduler();
    }

    public async Task DeleteResourceAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var resource = await db.Resources.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (resource != null)
        {
            db.Resources.Remove(resource);
            await db.SaveChangesAsync(cancellationToken);
        }

        await using var schedulerDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
        new SchedulingEngine(schedulerDb, _timeProvider).RunScheduler();
    }

    // ─────────────────────────────────────────────────────────
    // Adjustments
    // ─────────────────────────────────────────────────────────

    public async Task<List<Adjustment>> GetAdjustmentsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _readOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Adjustments.ToListAsync(cancellationToken);
    }

    public async Task AddAdjustmentAsync(
        string resourceId, string adjType, double availabilityPct,
        DateTime adjStart, DateTime adjEnd, string? notes,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var teamMember = await db.Resources
            .Include(r => r.Adjustments)
            .FirstOrDefaultAsync(r => r.ResourceId == resourceId, cancellationToken)
            ?? throw new DomainException($"Resource '{resourceId}' not found.");

        teamMember.AddAdjustment(adjType, availabilityPct, adjStart, adjEnd, notes);

        await db.SaveChangesAsync(cancellationToken);
        await db.DispatchDomainEventsAsync(_publisher, cancellationToken);

        await using var schedulerDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
        new SchedulingEngine(schedulerDb, _timeProvider).RunScheduler();
    }

    public async Task DeleteAdjustmentAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var adjustment = await db.Adjustments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (adjustment != null)
        {
            var teamMember = await db.Resources
                .Include(r => r.Adjustments)
                .FirstOrDefaultAsync(r => r.ResourceId == adjustment.ResourceId, cancellationToken);

            teamMember?.RemoveAdjustment(id);
            db.Adjustments.Remove(adjustment);
            await db.SaveChangesAsync(cancellationToken);
            await db.DispatchDomainEventsAsync(_publisher, cancellationToken);
        }

        await using var schedulerDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
        new SchedulingEngine(schedulerDb, _timeProvider).RunScheduler();
    }

    // ─────────────────────────────────────────────────────────
    // Holidays
    // ─────────────────────────────────────────────────────────

    public async Task<List<Holiday>> GetHolidaysAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _readOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Holidays.OrderBy(h => h.StartDate).ToListAsync(cancellationToken);
    }

    public async Task UpsertHolidayAsync(
        int id, string holidayName, DateTime startDate, DateTime endDate,
        string holidayType, string? notes, bool isNew,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        if (isNew)
        {
            var holiday = Holiday.Create(holidayName, startDate, endDate, holidayType, notes);
            db.Holidays.Add(holiday);
        }
        else
        {
            var existing = await db.Holidays.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
            existing?.Update(holidayName, startDate, endDate, holidayType, notes);
        }

        await db.SaveChangesAsync(cancellationToken);
        await db.DispatchDomainEventsAsync(_publisher, cancellationToken);

        await using var schedulerDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
        new SchedulingEngine(schedulerDb, _timeProvider).RunScheduler();
    }

    public async Task DeleteHolidayAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var holiday = await db.Holidays.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
        if (holiday != null)
        {
            db.Holidays.Remove(holiday);
            await db.SaveChangesAsync(cancellationToken);
        }

        await using var schedulerDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
        new SchedulingEngine(schedulerDb, _timeProvider).RunScheduler();
    }

    public async Task<bool> HasHolidayOverlapAsync(
        DateTime startDate, DateTime endDate, int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _readOnlyDbFactory.CreateDbContextAsync(cancellationToken);

        var query = db.Holidays
            .Where(h => h.StartDate.Date <= endDate.Date && h.EndDate.Date >= startDate.Date);

        if (excludeId.HasValue)
            query = query.Where(h => h.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<int> CopyHolidaysToYearAsync(
        int sourceYear, int targetYear,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var sourceHolidays = await db.Holidays
            .Where(h => h.StartDate.Year == sourceYear)
            .ToListAsync(cancellationToken);

        var yearDelta = targetYear - sourceYear;
        var copied = 0;

        foreach (var src in sourceHolidays)
        {
            var newStart = src.StartDate.AddYears(yearDelta);
            var newEnd = src.EndDate.AddYears(yearDelta);

            var overlaps = await db.Holidays
                .AnyAsync(h => h.StartDate.Date <= newEnd.Date && h.EndDate.Date >= newStart.Date, cancellationToken);

            if (!overlaps)
            {
                var holiday = Holiday.Create(src.HolidayName, newStart, newEnd, src.HolidayType, src.Notes);
                db.Holidays.Add(holiday);
                copied++;
            }
        }

        if (copied > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            await db.DispatchDomainEventsAsync(_publisher, cancellationToken);

            await using var schedulerDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
            new SchedulingEngine(schedulerDb, _timeProvider).RunScheduler();
        }

        return copied;
    }

    public async Task<int> GetHolidayWorkingDaysLostAsync(
        DateTime startDate, DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _readOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        var weekSetting = await db.Settings
            .FirstOrDefaultAsync(s => s.Key == DomainConstants.SettingKeys.WorkingWeek, cancellationToken);
        var weekendDays = DomainConstants.WorkingWeek.GetWeekendDays(
            weekSetting?.Value ?? DomainConstants.WorkingWeek.SunThu);

        int count = 0;
        var current = startDate.Date;
        while (current <= endDate.Date)
        {
            if (!weekendDays.Contains(current.DayOfWeek))
                count++;
            current = current.AddDays(1);
        }
        return count;
    }

    // ─────────────────────────────────────────────────────────
    // Calendar
    // ─────────────────────────────────────────────────────────

    public async Task<List<CalendarDay>> GetCalendarAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _readOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Calendar.OrderBy(c => c.CalendarDate).ToListAsync(cancellationToken);
    }

    // ─────────────────────────────────────────────────────────
    // Timeline
    // ─────────────────────────────────────────────────────────

    public async Task<TimelineDataDto> GetTimelineDataAsync(
        string resourceId,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _readOnlyDbFactory.CreateDbContextAsync(cancellationToken);

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
            var dayOfWeek = (int)current.DayOfWeek;
            var isWeekend = dayOfWeek == 5 || dayOfWeek == 6;
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

            string bgColor;
            string statusText;

            if (isWeekend)
            {
                bgColor = "#f8f9fa";
                statusText = dayOfWeek == 5 ? "Friday" : "Saturday";
            }
            else if (isHoliday)
            {
                bgColor = "#fff3cd";
                var h = holidays.First(x => x.StartDate.Date <= current.Date && x.EndDate.Date >= current.Date);
                statusText = h.HolidayName.Length > 10
                    ? h.HolidayName.Substring(0, 10) + ".."
                    : h.HolidayName;
            }
            else if (adjustment != null)
            {
                bgColor = "#cce5ff";
                statusText = adjustment.AdjType;
            }
            else if (workingTask != null)
            {
                bgColor = "#d1ecf1";
                statusText = workingTask.ServiceName.Length > 12
                    ? workingTask.ServiceName.Substring(0, 12) + ".."
                    : workingTask.ServiceName;
            }
            else
            {
                bgColor = "#d4edda";
                statusText = "Free";
            }

            days.Add(new TimelineDayDto(
                Date: current,
                DateDisplay: $"{current.Day} {current.ToString("ddd")}",
                BackgroundColor: bgColor,
                StatusText: statusText));

            current = current.AddDays(1);
        }

        return new TimelineDataDto(days);
    }

    // ─────────────────────────────────────────────────────────
    // Output Plan
    // ─────────────────────────────────────────────────────────

    public async Task<List<OutputPlanRowDto>> GetOutputPlanAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var scheduler = new SchedulingEngine(db, _timeProvider);
        return scheduler.GetOutputPlan();
    }
}
