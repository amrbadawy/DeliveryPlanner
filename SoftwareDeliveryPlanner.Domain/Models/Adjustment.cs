using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.SharedKernel;
using SoftwareDeliveryPlanner.Domain.SharedKernel.ValueObjects;

namespace SoftwareDeliveryPlanner.Domain.Models;

public class Adjustment
{
    public int Id { get; set; }
    public string ResourceId { get; set; } = string.Empty;
    public DateTime AdjStart { get; set; }
    public DateTime AdjEnd { get; set; }
    public double AvailabilityPct { get; set; }
    public string AdjType { get; set; } = DomainConstants.AdjustmentType.Other;
    public string? Notes { get; set; }

    // Navigation property
    public TeamMember? Resource { get; set; }

    // ── Domain factory ────────────────────────────────────────────────────────
    /// <summary>
    /// Creates and validates a new <see cref="Adjustment"/> using domain invariants.
    /// Raises <see cref="DomainException"/> on any violation.
    /// </summary>
    public static Adjustment Create(
        string resourceId,
        string adjType,
        double availabilityPct,
        DateTime adjStart,
        DateTime adjEnd,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
            throw new DomainException("Resource ID must not be empty.");

        if (!Percentage.TryCreate(availabilityPct, out _))
            throw new DomainException("Availability percentage must be between 0 and 100.");

        if (!DateRange.TryCreate(adjStart, adjEnd, out _))
            throw new DomainException("Adjustment start date must be on or before the end date.");

        return new Adjustment
        {
            ResourceId = resourceId.Trim(),
            AdjType = adjType,
            AvailabilityPct = availabilityPct,
            AdjStart = adjStart,
            AdjEnd = adjEnd,
            Notes = notes
        };
    }
}
