using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Models;

public class TaskNote
{
    public int Id { get; private set; }
    public string TaskId { get; private set; } = string.Empty;
    public string NoteText { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public string Author { get; private set; } = string.Empty;

    private TaskNote() { }

    public static TaskNote Create(string taskId, string noteText, string author)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            throw new DomainException("Task ID must not be empty.");

        if (string.IsNullOrWhiteSpace(noteText))
            throw new DomainException("Note text must not be empty.");

        if (string.IsNullOrWhiteSpace(author))
            throw new DomainException("Author must not be empty.");

        return new TaskNote
        {
            TaskId = taskId.Trim(),
            NoteText = noteText.Trim(),
            Author = author.Trim(),
            CreatedAt = TimeProvider.System.GetUtcNow().DateTime
        };
    }
}
