using SoftwareDeliveryPlanner.Domain.SharedKernel;
using ResourceIdVO = SoftwareDeliveryPlanner.Domain.SharedKernel.ValueObjects.ResourceId;
using PercentageVO = SoftwareDeliveryPlanner.Domain.SharedKernel.ValueObjects.Percentage;

namespace SoftwareDeliveryPlanner.Models;

public class TeamMember
{
    public int Id { get; set; }
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string Role { get; set; } = "Developer";
    public string Team { get; set; } = "Delivery";
    public double AvailabilityPct { get; set; } = 100.0;
    public double DailyCapacity { get; set; } = 1.0;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Active { get; set; } = "Yes";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // ── Domain factory ────────────────────────────────────────────────────────
    /// <summary>
    /// Creates and validates a new <see cref="TeamMember"/> using domain invariants.
    /// Raises <see cref="DomainException"/> on any violation.
    /// </summary>
    public static TeamMember Create(
        string resourceId,
        string resourceName,
        string role,
        string team,
        double availabilityPct,
        double dailyCapacity,
        DateTime startDate,
        string active = "Yes",
        string? notes = null)
    {
        if (!ResourceIdVO.TryCreate(resourceId, out _))
            throw new DomainException($"Invalid Resource ID '{resourceId}'. Expected format: AAA-000.");

        if (string.IsNullOrWhiteSpace(resourceName))
            throw new DomainException("Resource name must not be empty.");

        if (!PercentageVO.TryCreate(availabilityPct, out _))
            throw new DomainException("Availability percentage must be between 0 and 100.");

        if (dailyCapacity <= 0)
            throw new DomainException("Daily capacity must be greater than zero.");

        return new TeamMember
        {
            ResourceId = resourceId.Trim().ToUpperInvariant(),
            ResourceName = resourceName.Trim(),
            Role = role,
            Team = team,
            AvailabilityPct = availabilityPct,
            DailyCapacity = dailyCapacity,
            StartDate = startDate,
            Active = active,
            Notes = notes
        };
    }
}
