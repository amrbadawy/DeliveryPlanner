using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Events;

/// <summary>Raised when an existing <see cref="Models.Holiday"/> is modified.</summary>
public sealed record HolidayUpdatedEvent(int HolidayId) : DomainEvent;
