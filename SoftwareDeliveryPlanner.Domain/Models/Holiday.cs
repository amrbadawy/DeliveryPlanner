using SoftwareDeliveryPlanner.Domain.SharedKernel;

namespace SoftwareDeliveryPlanner.Models;

public class Holiday
{
    public int Id { get; set; }
    public string HolidayName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string HolidayType { get; set; } = "National";
    public string? Notes { get; set; }

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
        string holidayType = "National",
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(holidayName))
            throw new DomainException("Holiday name must not be empty.");

        if (startDate.Date > endDate.Date)
            throw new DomainException("Start date must be on or before end date.");

        return new Holiday
        {
            HolidayName = holidayName.Trim(),
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            HolidayType = holidayType,
            Notes = notes
        };
    }

    /// <summary>Single-day convenience overload.</summary>
    public static Holiday Create(
        string holidayName,
        DateTime date,
        string holidayType = "National",
        string? notes = null)
        => Create(holidayName, date, date, holidayType, notes);
}
