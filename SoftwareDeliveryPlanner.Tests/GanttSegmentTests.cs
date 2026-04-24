using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Data;
using SoftwareDeliveryPlanner.Infrastructure.Services;
using SoftwareDeliveryPlanner.SharedKernel;
using SoftwareDeliveryPlanner.Tests.Infrastructure;
using Xunit;

namespace SoftwareDeliveryPlanner.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class GanttSegmentTests : IAsyncDisposable
{
    private readonly SqlServerFixture _fixture;
    private readonly IDbContextFactory<PlannerDbContext> _factory;
    private readonly IPlanningQueryService _queryService;
    private readonly ISchedulerService _schedulerService;

    public GanttSegmentTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
        var (options, connectionString) = TestDatabaseHelper.CreateOptions(fixture);
        _factory = new TestDbContextFactory(options);
        var readOnlyFactory = new TestReadOnlyDbContextFactory(connectionString);
        var engineFactory = new SchedulingEngineFactory(_factory, TimeProvider.System);
        var publisher = new NullPublisher();

        _schedulerService = new SchedulerService(_factory, readOnlyFactory, engineFactory, publisher);
        _queryService = new PlanningQueryService(_factory, readOnlyFactory, engineFactory, publisher);
    }

    [Fact]
    public async Task GetGanttSegmentsAsync_ShouldIncludeEstimatedSegments_ForRolesWithoutResources()
    {
        // Arrange: Seed a task with BA effort (no BA resources exist in the seeded team)
        var taskId = "TST-001";
        var breakdown = new List<EffortBreakdownSpec>
        {
            new("DEV", 10, 0, 1.0),
            new("BA", 3, 0, 1.0),
            new("QA", 2, 0, 1.0)
        };

        await using var db = await _factory.CreateDbContextAsync();
        if (!await db.Tasks.AnyAsync(t => t.TaskId == taskId))
        {
            db.Tasks.Add(TaskItem.Create(taskId, "Gantt Test Task", 5, breakdown));
            await db.SaveChangesAsync();
        }

        // Run scheduler so tasks get PlannedStart/PlannedFinish
        await _schedulerService.RunSchedulerAsync(CancellationToken.None);

        // Act
        var segments = await _queryService.GetGanttSegmentsAsync(CancellationToken.None);

        var testTask = segments.FirstOrDefault(s => s.TaskId == taskId)?.Segments ?? new();

        var testTaskRoles = testTask.Select(s => s.Role).ToHashSet();

        // TST-001 has DEV+BA+QA effort
        Assert.Contains("DEV", testTaskRoles);
        Assert.Contains("BA", testTaskRoles);
        Assert.Contains("QA", testTaskRoles);

        // BA has no resources allocated, so should appear as estimated segment
        var testTaskEstimated = testTask.Where(s => s.IsEstimated).Select(s => s.Role).ToList();
        Assert.Contains("BA", testTaskEstimated);

        // QA also has no resources in test fixture, so it will also be estimated
        // (only DEV resources exist in test fixture)
    }

    public async ValueTask DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    private sealed class NullPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
    }
}