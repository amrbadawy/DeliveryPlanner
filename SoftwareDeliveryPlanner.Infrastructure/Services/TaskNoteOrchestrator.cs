using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Data;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal sealed class TaskNoteOrchestrator : ServiceBase, ITaskNoteOrchestrator
{
    public TaskNoteOrchestrator(
        IDbContextFactory<PlannerDbContext> dbFactory,
        IDbContextFactory<ReadOnlyPlannerDbContext> readOnlyDbFactory,
        ISchedulingEngineFactory engineFactory,
        IPublisher publisher)
        : base(dbFactory, readOnlyDbFactory, engineFactory, publisher) { }

    public async Task<List<TaskNote>> GetNotesAsync(string taskId)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync();
        return await db.TaskNotes
            .Where(n => n.TaskId == taskId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task AddNoteAsync(TaskNote note)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        db.TaskNotes.Add(note);
        await db.SaveChangesAsync();
    }

    public async Task DeleteNoteAsync(int id)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var note = await db.TaskNotes.FirstOrDefaultAsync(n => n.Id == id);
        if (note != null)
        {
            db.TaskNotes.Remove(note);
            await db.SaveChangesAsync();
        }
    }
}
