using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Events;

/// <summary>Raised when a new <see cref="Models.Holiday"/> is created.</summary>
public sealed record HolidayCreatedEvent(string HolidayName, DateTime StartDate) : DomainEvent;
