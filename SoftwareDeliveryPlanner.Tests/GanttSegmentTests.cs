using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain;
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

    [Fact]
    public async Task RunScheduler_SkipsPhaseWithNoResources_UnblocksDownstreamPhases()
    {
        // Arrange: Task with BA→DEV→QA pipeline. No BA resources exist.
        // Before the auto-skip fix, DEV would be permanently blocked by BA.
        var taskId = "SKP-001";
        var breakdown = new List<EffortBreakdownSpec>
        {
            new("BA", 3, 0, 1.0),
            new("DEV", 5, 0, 1.0),
            new("QA", 2, 0, 1.0)
        };

        await using (var db = await _factory.CreateDbContextAsync())
        {
            if (!await db.Tasks.AnyAsync(t => t.TaskId == taskId))
            {
                db.Tasks.Add(TaskItem.Create(taskId, "Auto-Skip Test Task", 5, breakdown));
                await db.SaveChangesAsync();
            }
        }

        // Act
        await _schedulerService.RunSchedulerAsync(CancellationToken.None);

        // Assert: task should be scheduled (PlannedStart not null)
        await using (var db = await _factory.CreateDbContextAsync())
        {
            var task = await db.Tasks.FirstAsync(t => t.TaskId == taskId);
            Assert.NotNull(task.PlannedStart);
            Assert.NotNull(task.PlannedFinish);
            Assert.NotEqual(DomainConstants.TaskStatus.NotStarted, task.Status);

            // DEV allocations should exist (phase was unblocked by BA auto-skip)
            var devAllocs = await db.Allocations
                .Where(a => a.TaskId == taskId && a.Role == "DEV" && a.HoursAllocated > 0)
                .ToListAsync();
            Assert.NotEmpty(devAllocs);

            // BA allocations should NOT exist (no BA resources)
            var baAllocs = await db.Allocations
                .Where(a => a.TaskId == taskId && a.Role == "BA" && a.HoursAllocated > 0)
                .ToListAsync();
            Assert.Empty(baAllocs);
        }
    }

    [Fact]
    public async Task RunScheduler_DoesNotSkipPhase_WhenResourcesExist()
    {
        // Arrange: Add a BA resource, then create task with BA→DEV→QA.
        // BA should be allocated normally (not skipped).
        var taskId = "NSK-001";
        var breakdown = new List<EffortBreakdownSpec>
        {
            new("BA", 2, 0, 1.0),
            new("DEV", 5, 0, 1.0),
            new("QA", 2, 0, 1.0)
        };

        await using (var db = await _factory.CreateDbContextAsync())
        {
            // Add a BA resource so the scheduler can allocate the BA phase
            if (!await db.Resources.AnyAsync(r => r.ResourceId == "BAT-001"))
            {
                db.Resources.Add(TeamMember.Create("BAT-001", "Test BA", DomainConstants.ResourceRole.BA,
                    DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 4, 12)));
                await db.SaveChangesAsync();
            }

            if (!await db.Tasks.AnyAsync(t => t.TaskId == taskId))
            {
                db.Tasks.Add(TaskItem.Create(taskId, "No-Skip Test Task", 5, breakdown));
                await db.SaveChangesAsync();
            }
        }

        // Act
        await _schedulerService.RunSchedulerAsync(CancellationToken.None);

        // Assert: all three roles should have allocations
        await using (var db = await _factory.CreateDbContextAsync())
        {
            var baAllocs = await db.Allocations
                .Where(a => a.TaskId == taskId && a.Role == "BA" && a.HoursAllocated > 0)
                .ToListAsync();
            Assert.NotEmpty(baAllocs); // BA resource exists — phase allocated, not skipped

            var devAllocs = await db.Allocations
                .Where(a => a.TaskId == taskId && a.Role == "DEV" && a.HoursAllocated > 0)
                .ToListAsync();
            Assert.NotEmpty(devAllocs);

            // Gantt segments should include all 3 roles as non-estimated
            var segments = await _queryService.GetGanttSegmentsAsync(CancellationToken.None);
            var taskSegs = segments.First(s => s.TaskId == taskId).Segments;
            var allocatedRoles = taskSegs.Where(s => !s.IsEstimated).Select(s => s.Role).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.Contains("BA", allocatedRoles);
            Assert.Contains("DEV", allocatedRoles);
        }
    }

    [Fact]
    public async Task RunScheduler_MidPipelineGap_SkipsOnlyMissingRole()
    {
        // Arrange: Task with BA→SA→DEV→QA. BA resource exists, SA does not.
        // SA should be auto-skipped; BA should be allocated; DEV/QA should proceed.
        var taskId = "MID-001";
        var breakdown = new List<EffortBreakdownSpec>
        {
            new("BA", 2, 0, 1.0),
            new("SA", 2, 0, 1.0),
            new("DEV", 5, 0, 1.0),
            new("QA", 2, 0, 1.0)
        };

        await using (var db = await _factory.CreateDbContextAsync())
        {
            // Ensure BA resource exists (may already exist from previous test)
            if (!await db.Resources.AnyAsync(r => r.ResourceId == "BAT-001"))
            {
                db.Resources.Add(TeamMember.Create("BAT-001", "Test BA", DomainConstants.ResourceRole.BA,
                    DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 4, 12)));
                await db.SaveChangesAsync();
            }

            if (!await db.Tasks.AnyAsync(t => t.TaskId == taskId))
            {
                db.Tasks.Add(TaskItem.Create(taskId, "Mid-Gap Test Task", 5, breakdown));
                await db.SaveChangesAsync();
            }
        }

        // Act
        await _schedulerService.RunSchedulerAsync(CancellationToken.None);

        // Assert
        await using (var db = await _factory.CreateDbContextAsync())
        {
            var task = await db.Tasks.FirstAsync(t => t.TaskId == taskId);
            Assert.NotNull(task.PlannedStart);

            // BA should have allocations (resource exists)
            var baAllocs = await db.Allocations
                .Where(a => a.TaskId == taskId && a.Role == "BA" && a.HoursAllocated > 0)
                .ToListAsync();
            Assert.NotEmpty(baAllocs);

            // SA should have NO allocations (auto-skipped, no resources)
            var saAllocs = await db.Allocations
                .Where(a => a.TaskId == taskId && a.Role == "SA" && a.HoursAllocated > 0)
                .ToListAsync();
            Assert.Empty(saAllocs);

            // DEV should have allocations (unblocked after SA auto-skip)
            var devAllocs = await db.Allocations
                .Where(a => a.TaskId == taskId && a.Role == "DEV" && a.HoursAllocated > 0)
                .ToListAsync();
            Assert.NotEmpty(devAllocs);

            // SA should appear as estimated segment in Gantt data
            var segments = await _queryService.GetGanttSegmentsAsync(CancellationToken.None);
            var taskSegs = segments.First(s => s.TaskId == taskId).Segments;
            var saSegment = taskSegs.FirstOrDefault(s => s.Role.Equals("SA", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(saSegment);
            Assert.True(saSegment.IsEstimated);
        }
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