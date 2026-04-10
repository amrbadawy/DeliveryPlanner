using SoftwareDeliveryPlanner.Domain.SharedKernel;

namespace SoftwareDeliveryPlanner.Models;

public class Holiday
{
    public int Id { get; set; }
    public string HolidayName { get; set; } = string.Empty;
    public DateTime HolidayDate { get; set; }
    public string HolidayType { get; set; } = "National";
    public string? Notes { get; set; }

    // ── Domain factory ────────────────────────────────────────────────────────
    /// <summary>
    /// Creates and validates a new <see cref="Holiday"/> using domain invariants.
    /// Raises <see cref="DomainException"/> on any violation.
    /// </summary>
    public static Holiday Create(
        string holidayName,
        DateTime holidayDate,
        string holidayType = "National",
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(holidayName))
            throw new DomainException("Holiday name must not be empty.");

        return new Holiday
        {
            HolidayName = holidayName.Trim(),
            HolidayDate = holidayDate.Date,
            HolidayType = holidayType,
            Notes = notes
        };
    }
}
