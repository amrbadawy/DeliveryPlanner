using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.SharedKernel;
using SoftwareDeliveryPlanner.SharedKernel.ValueObjects;

namespace SoftwareDeliveryPlanner.Domain.Models;

public class Adjustment
{
    public int Id { get; private set; }
    public string ResourceId { get; private set; } = string.Empty;
    public DateTime AdjStart { get; private set; }
    public DateTime AdjEnd { get; private set; }
    public double AvailabilityPct { get; private set; }
    public string AdjType { get; private set; } = DomainConstants.AdjustmentType.Other;
    public string? Notes { get; private set; }

    // ── Domain factory ────────────────────────────────────────────────────────
    /// <summary>
    /// Creates and validates a new <see cref="Adjustment"/> using domain invariants.
    /// Called internally by <see cref="TeamMember.AddAdjustment"/>.
    /// Raises <see cref="DomainException"/> on any violation.
    /// </summary>
    internal static Adjustment Create(
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
