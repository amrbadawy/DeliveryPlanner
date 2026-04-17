using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Events;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Models;

public class Holiday : AggregateRoot
{
    public int Id { get; private set; }
    public string HolidayName { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public string HolidayType { get; private set; } = DomainConstants.HolidayType.National;
    public string? Notes { get; private set; }

    /// <summary>Number of calendar days spanned (inclusive).</summary>
    public int DurationDays => (EndDate.Date - StartDate.Date).Days + 1;

    // ── Domain factory ────────────────────────────────────────────────────────
    /// <summary>
    /// Creates and validates a new <see cref="Holiday"/> using domain invariants.
    /// Raises <see cref="DomainException"/> on any violation.
    /// </summary>
    public static Holiday Create(
        string holidayName,
        DateTime startDate,
        DateTime endDate,
        string holidayType = DomainConstants.HolidayType.National,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(holidayName))
            throw new DomainException("Holiday name must not be empty.");

        if (startDate.Date > endDate.Date)
            throw new DomainException("Start date must be on or before end date.");

        var holiday = new Holiday
        {
            HolidayName = holidayName.Trim(),
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            HolidayType = holidayType,
            Notes = notes
        };

        holiday.RaiseDomainEvent(new HolidayCreatedEvent(holiday.HolidayName, holiday.StartDate));
        return holiday;
    }

    /// <summary>Single-day convenience overload.</summary>
    public static Holiday Create(
        string holidayName,
        DateTime date,
        string holidayType = DomainConstants.HolidayType.National,
        string? notes = null)
        => Create(holidayName, date, date, holidayType, notes);

    // ── Domain mutation ───────────────────────────────────────────────────────
    /// <summary>
    /// Updates user-editable properties and raises <see cref="HolidayUpdatedEvent"/>.
    /// </summary>
    public void Update(
        string holidayName,
        DateTime startDate,
        DateTime endDate,
        string holidayType,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(holidayName))
            throw new DomainException("Holiday name must not be empty.");

        if (startDate.Date > endDate.Date)
            throw new DomainException("Start date must be on or before end date.");

        HolidayName = holidayName.Trim();
        StartDate = startDate.Date;
        EndDate = endDate.Date;
        HolidayType = holidayType;
        Notes = notes;

        RaiseDomainEvent(new HolidayUpdatedEvent(Id));
    }
}
