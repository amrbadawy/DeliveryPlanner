using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Events;

/// <summary>Raised when a new <see cref="Models.TaskNote"/> is added.</summary>
public sealed record TaskNoteAddedEvent(string TaskId, string NoteText) : DomainEvent;
