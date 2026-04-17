using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Data;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal sealed class HolidayService : ServiceBase, IHolidayOrchestrator
{
    public HolidayService(
        IDbContextFactory<PlannerDbContext> dbFactory,
        IDbContextFactory<ReadOnlyPlannerDbContext> readOnlyDbFactory,
        ISchedulingEngineFactory engineFactory,
        IPublisher publisher)
        : base(dbFactory, readOnlyDbFactory, engineFactory, publisher) { }

    public async Task<List<Holiday>> GetHolidaysAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Holidays.OrderBy(h => h.StartDate).ToListAsync(cancellationToken);
    }

    public async Task UpsertHolidayAsync(
        int id, string holidayName, DateTime startDate, DateTime endDate,
        string holidayType, string? notes, bool isNew,
        CancellationToken cancellationToken = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(cancellationToken);

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

        await SaveDispatchAndRescheduleAsync(db, cancellationToken);
    }

    public async Task DeleteHolidayAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(cancellationToken);
        var holiday = await db.Holidays.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
        if (holiday != null)
            db.Holidays.Remove(holiday);

        await SaveDispatchAndRescheduleAsync(db, cancellationToken);
    }

    public async Task<bool> HasHolidayOverlapAsync(
        DateTime startDate, DateTime endDate, int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);

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
        await using var db = await DbFactory.CreateDbContextAsync(cancellationToken);

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
            await SaveDispatchAndRescheduleAsync(db, cancellationToken);

        return copied;
    }

    public async Task<int> GetHolidayWorkingDaysLostAsync(
        DateTime startDate, DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
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
}
