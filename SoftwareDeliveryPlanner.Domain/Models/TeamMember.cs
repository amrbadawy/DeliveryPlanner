using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Events;
using SoftwareDeliveryPlanner.SharedKernel;
using SoftwareDeliveryPlanner.SharedKernel.ValueObjects;
using ResourceIdVO = SoftwareDeliveryPlanner.SharedKernel.ValueObjects.ResourceId;
using PercentageVO = SoftwareDeliveryPlanner.SharedKernel.ValueObjects.Percentage;

namespace SoftwareDeliveryPlanner.Domain.Models;

public class TeamMember : AggregateRoot
{
    private readonly List<Adjustment> _adjustments = [];

    public int Id { get; private set; }
    public string ResourceId { get; private set; } = string.Empty;
    public string ResourceName { get; private set; } = string.Empty;
    public string Role { get; private set; } = DomainConstants.ResourceRole.Developer;
    public string Team { get; private set; } = DomainConstants.DefaultTeam;
    public double AvailabilityPct { get; private set; } = 100.0;
    public double DailyCapacity { get; private set; } = 1.0;
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public string Active { get; private set; } = DomainConstants.ActiveStatus.Yes;
    public string? Notes { get; private set; }

    /// <summary>Seniority level of the team member (Junior, Mid, Senior, Principal).</summary>
    public string SeniorityLevel { get; private set; } = DomainConstants.Seniority.Mid;

    /// <summary>Working week override for this team member. Null means use the global default.</summary>
    public string? WorkingWeek { get; private set; }

    public DateTime CreatedAt { get; private set; } = TimeProvider.System.GetLocalNow().DateTime;

    /// <summary>Adjustments owned by this aggregate. Managed via <see cref="AddAdjustment"/> / <see cref="RemoveAdjustment"/>.</summary>
    public IReadOnlyCollection<Adjustment> Adjustments => _adjustments.AsReadOnly();

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
        DateTime? endDate = null,
        string active = DomainConstants.ActiveStatus.Yes,
        string? notes = null,
        string seniorityLevel = DomainConstants.Seniority.Mid,
        string? workingWeek = null)
    {
        if (!ResourceIdVO.TryCreate(resourceId, out _))
            throw new DomainException($"Invalid Resource ID '{resourceId}'. Expected format: AAA-000.");

        if (string.IsNullOrWhiteSpace(resourceName))
            throw new DomainException("Resource name must not be empty.");

        if (!PercentageVO.TryCreate(availabilityPct, out _))
            throw new DomainException("Availability percentage must be between 0 and 100.");

        if (dailyCapacity <= 0)
            throw new DomainException("Daily capacity must be greater than zero.");

        if (!DomainConstants.Seniority.IsValid(seniorityLevel))
            throw new DomainException($"Invalid seniority level '{seniorityLevel}'. Valid levels: {string.Join(", ", DomainConstants.Seniority.Levels)}.");

        if (workingWeek is not null &&
            workingWeek != DomainConstants.WorkingWeek.SunThu &&
            workingWeek != DomainConstants.WorkingWeek.MonFri)
            throw new DomainException($"Invalid working week '{workingWeek}'. Valid values: {DomainConstants.WorkingWeek.SunThu}, {DomainConstants.WorkingWeek.MonFri}.");

        if (endDate.HasValue && endDate.Value < startDate)
            throw new DomainException("End date cannot be before start date.");

        var member = new TeamMember
        {
            ResourceId = resourceId.Trim().ToUpperInvariant(),
            ResourceName = resourceName.Trim(),
            Role = role,
            Team = team,
            AvailabilityPct = availabilityPct,
            DailyCapacity = dailyCapacity,
            StartDate = startDate,
            EndDate = endDate,
            Active = active,
            Notes = notes,
            SeniorityLevel = seniorityLevel,
            WorkingWeek = workingWeek
        };

        member.RaiseDomainEvent(new ResourceCreatedEvent(member.ResourceId, member.ResourceName));
        return member;
    }

    // ── Domain mutation ───────────────────────────────────────────────────────
    /// <summary>
    /// Updates user-editable properties and raises <see cref="ResourceUpdatedEvent"/>.
    /// </summary>
    public void Update(
        string resourceName,
        string role,
        string team,
        double availabilityPct,
        double dailyCapacity,
        DateTime startDate,
        string active,
        string? notes = null,
        string seniorityLevel = DomainConstants.Seniority.Mid,
        string? workingWeek = null)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
            throw new DomainException("Resource name must not be empty.");

        if (!PercentageVO.TryCreate(availabilityPct, out _))
            throw new DomainException("Availability percentage must be between 0 and 100.");

        if (dailyCapacity <= 0)
            throw new DomainException("Daily capacity must be greater than zero.");

        if (!DomainConstants.Seniority.IsValid(seniorityLevel))
            throw new DomainException($"Invalid seniority level '{seniorityLevel}'. Valid levels: {string.Join(", ", DomainConstants.Seniority.Levels)}.");

        if (workingWeek is not null &&
            workingWeek != DomainConstants.WorkingWeek.SunThu &&
            workingWeek != DomainConstants.WorkingWeek.MonFri)
            throw new DomainException($"Invalid working week '{workingWeek}'. Valid values: {DomainConstants.WorkingWeek.SunThu}, {DomainConstants.WorkingWeek.MonFri}.");

        ResourceName = resourceName.Trim();
        Role = role;
        Team = team;
        AvailabilityPct = availabilityPct;
        DailyCapacity = dailyCapacity;
        StartDate = startDate;
        Active = active;
        Notes = notes;
        SeniorityLevel = seniorityLevel;
        WorkingWeek = workingWeek;

        RaiseDomainEvent(new ResourceUpdatedEvent(ResourceId));
    }

    // ── Child entity management ───────────────────────────────────────────────
    /// <summary>
    /// Adds a new adjustment to this team member. Raises <see cref="AdjustmentAddedEvent"/>.
    /// </summary>
    public Adjustment AddAdjustment(
        string adjType,
        double availabilityPct,
        DateTime adjStart,
        DateTime adjEnd,
        string? notes = null)
    {
        var adjustment = Adjustment.Create(ResourceId, adjType, availabilityPct, adjStart, adjEnd, notes);
        _adjustments.Add(adjustment);

        RaiseDomainEvent(new AdjustmentAddedEvent(ResourceId, adjType));
        return adjustment;
    }

    /// <summary>
    /// Removes an adjustment by ID. Raises <see cref="AdjustmentRemovedEvent"/>.
    /// </summary>
    public void RemoveAdjustment(int adjustmentId)
    {
        var adjustment = _adjustments.FirstOrDefault(a => a.Id == adjustmentId)
            ?? throw new DomainException($"Adjustment {adjustmentId} not found for resource {ResourceId}.");

        _adjustments.Remove(adjustment);

        RaiseDomainEvent(new AdjustmentRemovedEvent(ResourceId, adjustmentId));
    }
}
