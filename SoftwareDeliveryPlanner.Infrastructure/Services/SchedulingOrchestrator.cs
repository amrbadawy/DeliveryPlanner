using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Data;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Models;
using SoftwareDeliveryPlanner.Services;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

public sealed class SchedulingOrchestrator : ISchedulingOrchestrator
{
    private readonly IDbContextFactory<PlannerDbContext> _dbFactory;

    public SchedulingOrchestrator(IDbContextFactory<PlannerDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // ─────────────────────────────────────────────────────────
    // Scheduler
    // ─────────────────────────────────────────────────────────

    public async Task<string> RunSchedulerAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var scheduler = new SchedulingEngine(db);
        return scheduler.RunScheduler();
    }

    // ─────────────────────────────────────────────────────────
    // Dashboard KPIs
    // ─────────────────────────────────────────────────────────

    public async Task<DashboardKpisDto> GetDashboardKpisAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var scheduler = new SchedulingEngine(db);
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
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Tasks.OrderBy(t => t.SchedulingRank ?? 999).ToListAsync(cancellationToken);
    }

    public async Task<int> GetTaskCountAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Tasks.CountAsync(cancellationToken);
    }

    public async Task UpsertTaskAsync(TaskItem task, bool isNew, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        if (isNew)
        {
            task.CreatedAt = DateTime.Now;
            task.UpdatedAt = DateTime.Now;
            db.Tasks.Add(task);
        }
        else
        {
            var existing = await db.Tasks.FirstOrDefaultAsync(t => t.Id == task.Id, cancellationToken);
            if (existing != null)
            {
                existing.ServiceName = task.ServiceName;
                existing.DevEstimation = task.DevEstimation;
                existing.MaxDev = task.MaxDev;
                existing.Priority = task.Priority;
                existing.StrictDate = task.StrictDate;
                existing.DependsOnTaskIds = task.DependsOnTaskIds;
                existing.UpdatedAt = DateTime.Now;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        await using var schedulerDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
        new SchedulingEngine(schedulerDb).RunScheduler();
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
        new SchedulingEngine(schedulerDb).RunScheduler();
    }

    // ─────────────────────────────────────────────────────────
    // Resources
    // ─────────────────────────────────────────────────────────

    public async Task<List<TeamMember>> GetResourcesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Resources.ToListAsync(cancellationToken);
    }

    public async Task<int> GetResourceCountAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Resources.CountAsync(cancellationToken);
    }

    public async Task UpsertResourceAsync(TeamMember resource, bool isNew, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        if (isNew)
        {
            resource.CreatedAt = DateTime.Now;
            db.Resources.Add(resource);
        }
        else
        {
            var existing = await db.Resources.FirstOrDefaultAsync(r => r.Id == resource.Id, cancellationToken);
            if (existing != null)
            {
                existing.ResourceName = resource.ResourceName;
                existing.Role = resource.Role;
                existing.Team = resource.Team;
                existing.AvailabilityPct = resource.AvailabilityPct;
                existing.DailyCapacity = resource.DailyCapacity;
                existing.StartDate = resource.StartDate;
                existing.Active = resource.Active;
                existing.Notes = resource.Notes;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        await using var schedulerDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
        new SchedulingEngine(schedulerDb).RunScheduler();
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
        new SchedulingEngine(schedulerDb).RunScheduler();
    }

    // ─────────────────────────────────────────────────────────
    // Adjustments
    // ─────────────────────────────────────────────────────────

    public async Task<List<Adjustment>> GetAdjustmentsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Adjustments.ToListAsync(cancellationToken);
    }

    public async Task AddAdjustmentAsync(Adjustment adjustment, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        db.Adjustments.Add(adjustment);
        await db.SaveChangesAsync(cancellationToken);

        await using var schedulerDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
        new SchedulingEngine(schedulerDb).RunScheduler();
    }

    public async Task DeleteAdjustmentAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var adjustment = await db.Adjustments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (adjustment != null)
        {
            db.Adjustments.Remove(adjustment);
            await db.SaveChangesAsync(cancellationToken);
        }

        await using var schedulerDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
        new SchedulingEngine(schedulerDb).RunScheduler();
    }

    // ─────────────────────────────────────────────────────────
    // Holidays
    // ─────────────────────────────────────────────────────────

    public async Task<List<Holiday>> GetHolidaysAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Holidays.OrderBy(h => h.StartDate).ToListAsync(cancellationToken);
    }

    public async Task UpsertHolidayAsync(Holiday holiday, bool isNew, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        if (isNew)
        {
            db.Holidays.Add(holiday);
        }
        else
        {
            var existing = await db.Holidays.FirstOrDefaultAsync(h => h.Id == holiday.Id, cancellationToken);
            if (existing != null)
            {
                existing.HolidayName = holiday.HolidayName;
                existing.StartDate = holiday.StartDate;
                existing.EndDate = holiday.EndDate;
                existing.HolidayType = holiday.HolidayType;
                existing.Notes = holiday.Notes;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        await using var schedulerDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
        new SchedulingEngine(schedulerDb).RunScheduler();
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
        new SchedulingEngine(schedulerDb).RunScheduler();
    }

    public async Task<bool> HasHolidayOverlapAsync(
        DateTime startDate, DateTime endDate, int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        // Overlap formula: A.StartDate <= B.EndDate AND A.EndDate >= B.StartDate
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

            // Check no overlap in target year
            var overlaps = await db.Holidays
                .AnyAsync(h => h.StartDate.Date <= newEnd.Date && h.EndDate.Date >= newStart.Date, cancellationToken);

            if (!overlaps)
            {
                db.Holidays.Add(new Holiday
                {
                    HolidayName = src.HolidayName,
                    StartDate = newStart,
                    EndDate = newEnd,
                    HolidayType = src.HolidayType,
                    Notes = src.Notes
                });
                copied++;
            }
        }

        if (copied > 0)
        {
            await db.SaveChangesAsync(cancellationToken);

            await using var schedulerDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
            new SchedulingEngine(schedulerDb).RunScheduler();
        }

        return copied;
    }

    public async Task<int> GetHolidayWorkingDaysLostAsync(
        DateTime startDate, DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
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
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
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
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

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
            // Date range holiday check: any holiday whose range covers this date
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

    public async Task<List<Dictionary<string, object?>>> GetOutputPlanAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var scheduler = new SchedulingEngine(db);
        return scheduler.GetOutputPlan();
    }
}
