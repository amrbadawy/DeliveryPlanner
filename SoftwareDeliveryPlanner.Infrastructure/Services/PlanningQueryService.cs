using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
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
                StatusText: statusText));

            current = current.AddDays(1);
        }

        return new TimelineDataDto(days);
    }
}
