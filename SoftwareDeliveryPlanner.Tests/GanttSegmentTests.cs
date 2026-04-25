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

    [Fact]
    public async Task RunScheduler_FutureStartDateWithinPlanHorizon_NotAutoSkipped()
    {
        // Arrange: BA resource joins 2 months into a 2-year plan window.
        // Plan starts 2026-05-01, BA joins 2026-07-01 → within horizon → NOT auto-skipped.
        var taskId = "FUT-001";
        var breakdown = TestDatabaseHelper.MakeMultiRoleBreakdown(5, 2, ("BA", 3));

        await using (var db = await _factory.CreateDbContextAsync())
        {
            if (!await db.Resources.AnyAsync(r => r.ResourceId == "BAF-001"))
            {
                db.Resources.Add(TeamMember.Create("BAF-001", "Future BA", DomainConstants.ResourceRole.BA,
                    DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 7, 1)));
                await db.SaveChangesAsync();
            }

            if (!await db.Tasks.AnyAsync(t => t.TaskId == taskId))
            {
                db.Tasks.Add(TaskItem.Create(taskId, "Future BA Task", 5, breakdown));
                await db.SaveChangesAsync();
            }
        }

        // Act
        await _schedulerService.RunSchedulerAsync(CancellationToken.None);

        // Assert: BA phase should be allocated (resource available from Jul 2026)
        await using (var db = await _factory.CreateDbContextAsync())
        {
            var baAllocs = await db.Allocations
                .Where(a => a.TaskId == taskId && a.Role == "BA" && a.HoursAllocated > 0)
                .ToListAsync();
            Assert.NotEmpty(baAllocs);

            // BA allocations should start on or after the resource's join date
            var firstBaDate = baAllocs.Min(a => a.CalendarDate);
            Assert.True(firstBaDate >= new DateTime(2026, 7, 1),
                $"BA allocations should not start before resource join date. First: {firstBaDate:yyyy-MM-dd}");

            // DEV should also be allocated (after BA completes)
            var devAllocs = await db.Allocations
                .Where(a => a.TaskId == taskId && a.Role == "DEV" && a.HoursAllocated > 0)
                .ToListAsync();
            Assert.NotEmpty(devAllocs);
        }
    }

    [Fact]
    public async Task RunScheduler_FutureStartDateBeyondPlanHorizon_AutoSkipped()
    {
        // Arrange: BA resource joins in 2030 — well beyond the plan window (2026-05-01 + 730 days).
        // Should be excluded from resourcesByRole → auto-skip fires → DEV/QA proceed.
        var taskId = "BYD-001";
        var breakdown = TestDatabaseHelper.MakeMultiRoleBreakdown(5, 2, ("BA", 3));

        await using (var db = await _factory.CreateDbContextAsync())
        {
            if (!await db.Resources.AnyAsync(r => r.ResourceId == "BAX-001"))
            {
                db.Resources.Add(TeamMember.Create("BAX-001", "Far Future BA", DomainConstants.ResourceRole.BA,
                    DomainConstants.DefaultTeam, 100, 1, new DateTime(2030, 1, 1)));
                await db.SaveChangesAsync();
            }

            if (!await db.Tasks.AnyAsync(t => t.TaskId == taskId))
            {
                db.Tasks.Add(TaskItem.Create(taskId, "Beyond Horizon Task", 5, breakdown));
                await db.SaveChangesAsync();
            }
        }

        // Act
        await _schedulerService.RunSchedulerAsync(CancellationToken.None);

        // Assert: BA auto-skipped, DEV/QA allocated
        await using (var db = await _factory.CreateDbContextAsync())
        {
            var task = await db.Tasks.FirstAsync(t => t.TaskId == taskId);
            Assert.NotNull(task.PlannedStart);

            var baAllocs = await db.Allocations
                .Where(a => a.TaskId == taskId && a.Role == "BA" && a.HoursAllocated > 0)
                .ToListAsync();
            Assert.Empty(baAllocs); // auto-skipped

            var devAllocs = await db.Allocations
                .Where(a => a.TaskId == taskId && a.Role == "DEV" && a.HoursAllocated > 0)
                .ToListAsync();
            Assert.NotEmpty(devAllocs); // unblocked
        }
    }

    [Fact]
    public async Task RunScheduler_ExpiredResourceBeforePlanStart_AutoSkipped()
    {
        // Arrange: SA resource with EndDate=2026-03-01, plan starts 2026-05-01.
        // Resource left before plan began → excluded from resourcesByRole → auto-skip fires.
        var taskId = "EXP-001";
        var breakdown = TestDatabaseHelper.MakeMultiRoleBreakdown(5, 2, ("SA", 3));

        await using (var db = await _factory.CreateDbContextAsync())
        {
            if (!await db.Resources.AnyAsync(r => r.ResourceId == "SAE-001"))
            {
                db.Resources.Add(TeamMember.Create("SAE-001", "Expired SA", DomainConstants.ResourceRole.SA,
                    DomainConstants.DefaultTeam, 100, 1, new DateTime(2025, 1, 1),
                    endDate: new DateTime(2026, 3, 1)));
                await db.SaveChangesAsync();
            }

            if (!await db.Tasks.AnyAsync(t => t.TaskId == taskId))
            {
                db.Tasks.Add(TaskItem.Create(taskId, "Expired Resource Task", 5, breakdown));
                await db.SaveChangesAsync();
            }
        }

        // Act
        await _schedulerService.RunSchedulerAsync(CancellationToken.None);

        // Assert: SA auto-skipped, DEV/QA proceed
        await using (var db = await _factory.CreateDbContextAsync())
        {
            var task = await db.Tasks.FirstAsync(t => t.TaskId == taskId);
            Assert.NotNull(task.PlannedStart);

            var saAllocs = await db.Allocations
                .Where(a => a.TaskId == taskId && a.Role == "SA" && a.HoursAllocated > 0)
                .ToListAsync();
            Assert.Empty(saAllocs); // auto-skipped

            var devAllocs = await db.Allocations
                .Where(a => a.TaskId == taskId && a.Role == "DEV" && a.HoursAllocated > 0)
                .ToListAsync();
            Assert.NotEmpty(devAllocs);
        }
    }

    [Fact]
    public async Task RunScheduler_RerunAfterAddingResource_PhaseGetsAllocated()
    {
        // Arrange: First run — no SA resources → SA auto-skipped.
        // Then add SA resource and re-run → SA should now be allocated.
        var taskId = "RRN-001";
        var breakdown = TestDatabaseHelper.MakeMultiRoleBreakdown(5, 2, ("SA", 2));

        await using (var db = await _factory.CreateDbContextAsync())
        {
            if (!await db.Tasks.AnyAsync(t => t.TaskId == taskId))
            {
                db.Tasks.Add(TaskItem.Create(taskId, "Rerun Test Task", 5, breakdown));
                await db.SaveChangesAsync();
            }
        }

        // First run — SA auto-skipped
        await _schedulerService.RunSchedulerAsync(CancellationToken.None);

        await using (var db = await _factory.CreateDbContextAsync())
        {
            var saAllocsBefore = await db.Allocations
                .Where(a => a.TaskId == taskId && a.Role == "SA" && a.HoursAllocated > 0)
                .ToListAsync();
            Assert.Empty(saAllocsBefore); // confirmed: auto-skipped
        }

        // Add SA resource
        await using (var db = await _factory.CreateDbContextAsync())
        {
            if (!await db.Resources.AnyAsync(r => r.ResourceId == "SAR-001"))
            {
                db.Resources.Add(TeamMember.Create("SAR-001", "New SA", DomainConstants.ResourceRole.SA,
                    DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 5, 1)));
                await db.SaveChangesAsync();
            }
        }

        // Re-run scheduler
        await _schedulerService.RunSchedulerAsync(CancellationToken.None);

        // Assert: SA should now have allocations
        await using (var db = await _factory.CreateDbContextAsync())
        {
            var saAllocsAfter = await db.Allocations
                .Where(a => a.TaskId == taskId && a.Role == "SA" && a.HoursAllocated > 0)
                .ToListAsync();
            Assert.NotEmpty(saAllocsAfter); // no longer auto-skipped

            var devAllocs = await db.Allocations
                .Where(a => a.TaskId == taskId && a.Role == "DEV" && a.HoursAllocated > 0)
                .ToListAsync();
            Assert.NotEmpty(devAllocs); // DEV still proceeds after SA
        }
    }

    [Fact]
    public async Task RunScheduler_DependencyWithAutoSkippedPredecessor_SuccessorUnblocked()
    {
        // Arrange: Task A (BA→DEV→QA, no BA resource) → Task B depends on A (FS).
        // BA auto-skip in A should not block B. B should start after A finishes.
        var taskIdA = "DPA-001";
        var taskIdB = "DPB-001";
        var breakdownA = TestDatabaseHelper.MakeMultiRoleBreakdown(5, 2, ("BA", 3));
        var breakdownB = TestDatabaseHelper.MakeBreakdown(4, 2);

        await using (var db = await _factory.CreateDbContextAsync())
        {
            if (!await db.Tasks.AnyAsync(t => t.TaskId == taskIdA))
            {
                db.Tasks.Add(TaskItem.Create(taskIdA, "Predecessor With Skip", 5, breakdownA));
                await db.SaveChangesAsync();
            }

            if (!await db.Tasks.AnyAsync(t => t.TaskId == taskIdB))
            {
                var taskB = TaskItem.Create(taskIdB, "Successor Task", 5, breakdownB);
                taskB.AddDependency(taskIdA, DomainConstants.DependencyType.FinishToStart);
                db.Tasks.Add(taskB);
                await db.SaveChangesAsync();
            }
        }

        // Act
        await _schedulerService.RunSchedulerAsync(CancellationToken.None);

        // Assert
        await using (var db = await _factory.CreateDbContextAsync())
        {
            var taskA = await db.Tasks.FirstAsync(t => t.TaskId == taskIdA);
            var taskB = await db.Tasks.FirstAsync(t => t.TaskId == taskIdB);

            Assert.NotNull(taskA.PlannedStart);
            Assert.NotNull(taskA.PlannedFinish);
            Assert.NotNull(taskB.PlannedStart);
            Assert.NotNull(taskB.PlannedFinish);

            // B must start on or after A finishes (FS dependency)
            Assert.True(taskB.PlannedStart >= taskA.PlannedFinish,
                $"Task B should start after A finishes. A.Finish={taskA.PlannedFinish:yyyy-MM-dd}, B.Start={taskB.PlannedStart:yyyy-MM-dd}");

            // Both tasks should have DEV allocations
            var devAllocsA = await db.Allocations
                .Where(a => a.TaskId == taskIdA && a.Role == "DEV" && a.HoursAllocated > 0)
                .ToListAsync();
            var devAllocsB = await db.Allocations
                .Where(a => a.TaskId == taskIdB && a.Role == "DEV" && a.HoursAllocated > 0)
                .ToListAsync();
            Assert.NotEmpty(devAllocsA);
            Assert.NotEmpty(devAllocsB);
        }
    }

    [Fact]
    public async Task RunScheduler_ZeroEffortPhase_RejectedByDomainValidation()
    {
        // Zero-effort phases are rejected by TaskEffortBreakdown.Create() — the domain
        // enforces estimationDays > 0.  This means the auto-skip path (which checks
        // phase.EstimationDays > 0) can never encounter a 0-effort phase at runtime.
        // Verify the domain guard is in place.
        var ex = Assert.Throws<DomainException>(() =>
            TaskItem.Create("ZRO-001", "Zero Effort Task", 5, new List<EffortBreakdownSpec>
            {
                new("BA", 0, 0, 1.0),
                new("DEV", 5, 0, 1.0),
                new("QA", 2, 0, 1.0)
            }));

        Assert.Contains("greater than zero", ex.Message);
        await Task.CompletedTask; // keep async signature for consistency
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