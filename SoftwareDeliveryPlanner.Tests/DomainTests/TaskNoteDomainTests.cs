using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Tests;

public class TaskNoteDomainTests
{
    [Fact]
    public void Create_ValidInputs_ReturnsPopulatedTaskNote()
    {
        var note = TaskNote.Create("SVC-001", "Test note", "Developer");

        Assert.Equal("SVC-001", note.TaskId);
        Assert.Equal("Test note", note.NoteText);
        Assert.Equal("Developer", note.Author);
    }

    [Fact]
    public void Create_SetsCreatedAtToApproximatelyNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var note = TaskNote.Create("SVC-001", "Test note", "Developer");
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.True(note.CreatedAt >= before && note.CreatedAt <= after);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyTaskId_ThrowsDomainException(string taskId)
    {
        Assert.Throws<DomainException>(() => TaskNote.Create(taskId, "Note", "Author"));
    }

    [Fact]
    public void Create_NullTaskId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => TaskNote.Create(null!, "Note", "Author"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyNoteText_ThrowsDomainException(string noteText)
    {
        Assert.Throws<DomainException>(() => TaskNote.Create("SVC-001", noteText, "Author"));
    }

    [Fact]
    public void Create_NullNoteText_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => TaskNote.Create("SVC-001", null!, "Author"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyAuthor_ThrowsDomainException(string author)
    {
        Assert.Throws<DomainException>(() => TaskNote.Create("SVC-001", "Note", author));
    }

    [Fact]
    public void Create_NullAuthor_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => TaskNote.Create("SVC-001", "Note", null!));
    }

    [Fact]
    public void Create_TrimsInputs()
    {
        var note = TaskNote.Create("  SVC-001  ", "  Note  ", "  Dev  ");

        Assert.Equal("SVC-001", note.TaskId);
        Assert.Equal("Note", note.NoteText);
        Assert.Equal("Dev", note.Author);
    }
}
