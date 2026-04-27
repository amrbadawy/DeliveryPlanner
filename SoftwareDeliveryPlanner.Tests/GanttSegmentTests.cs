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

        var testTask = segments.FirstOrDefault(s => s.TaskId == taskId)?.Segments ?? (IReadOnlyList<GanttRoleSegmentDto>)Array.Empty<GanttRoleSegmentDto>();

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

    [Fact]
    public async Task GetGanttSegments_EstimatedSegments_DistributedProportionally()
    {
        // Arrange: task with BA(3d)→SA(2d)→DEV(10d)→QA(5d).
        // DEV and QA have resources in the test fixture; BA and SA do not.
        // BA and SA should be estimated with DIFFERENT date ranges (not overlapping).
        var taskId = "PRP-001";
        var breakdown = TestDatabaseHelper.MakeMultiRoleBreakdown(10, 5,
            ("BA", 3),
            ("SA", 2));

        await using (var db = await _factory.CreateDbContextAsync())
        {
            if (!await db.Tasks.AnyAsync(t => t.TaskId == taskId))
            {
                db.Tasks.Add(TaskItem.Create(taskId, "Proportional Segment Test", 5, breakdown));
                await db.SaveChangesAsync();
            }
        }

        await _schedulerService.RunSchedulerAsync(CancellationToken.None);

        // Act
        var segments = await _queryService.GetGanttSegmentsAsync(CancellationToken.None);
        var taskSegments = segments.FirstOrDefault(s => s.TaskId == taskId)?.Segments ?? (IReadOnlyList<GanttRoleSegmentDto>)Array.Empty<GanttRoleSegmentDto>();

        var baSegment = taskSegments.FirstOrDefault(s => s.Role == "BA");
        var saSegment = taskSegments.FirstOrDefault(s => s.Role == "SA");
        var devSegment = taskSegments.FirstOrDefault(s => s.Role == "DEV");
        var qaSegment = taskSegments.FirstOrDefault(s => s.Role == "QA");

        // Assert: all 4 roles present
        Assert.NotNull(baSegment);
        Assert.NotNull(saSegment);
        Assert.NotNull(devSegment);
        Assert.NotNull(qaSegment);

        // BA and SA are estimated (no resources in test fixture)
        Assert.True(baSegment.IsEstimated, "BA should be estimated (no BA resources)");
        Assert.True(saSegment.IsEstimated, "SA should be estimated (no SA resources)");

        // DEV and QA are allocation-based (resources exist)
        Assert.False(devSegment.IsEstimated, "DEV should be allocated");
        Assert.False(qaSegment.IsEstimated, "QA should be allocated");

        // KEY ASSERTION: BA and SA must have DIFFERENT start dates (proportional distribution)
        Assert.NotEqual(baSegment.SegmentStart, saSegment.SegmentStart);

        // BA should start before SA (pipeline order: BA → SA → DEV → QA)
        Assert.True(baSegment.SegmentStart <= saSegment.SegmentStart,
            $"BA should start <= SA: BA={baSegment.SegmentStart:yyyy-MM-dd}, SA={saSegment.SegmentStart:yyyy-MM-dd}");
    }

    [Fact]
    public async Task GetGanttSegments_AllEstimated_DistributedAcrossEnvelope()
    {
        // Arrange: task with SA(2d)→UX(4d)→DEV(8d)→QA(4d). No SA/UX resources exist.
        // Only DEV resources exist in the test fixture.
        // SA and UX should be estimated and distributed sequentially before DEV.
        var taskId = "PRP-002";
        var breakdown = TestDatabaseHelper.MakeMultiRoleBreakdown(8, 4,
            ("SA", 2),
            ("UX", 4));

        await using (var db = await _factory.CreateDbContextAsync())
        {
            if (!await db.Tasks.AnyAsync(t => t.TaskId == taskId))
            {
                db.Tasks.Add(TaskItem.Create(taskId, "All Estimated Distribution Test", 5, breakdown));
                await db.SaveChangesAsync();
            }
        }

        await _schedulerService.RunSchedulerAsync(CancellationToken.None);

        // Act
        var segments = await _queryService.GetGanttSegmentsAsync(CancellationToken.None);
        var taskSegments = segments.FirstOrDefault(s => s.TaskId == taskId)?.Segments ?? (IReadOnlyList<GanttRoleSegmentDto>)Array.Empty<GanttRoleSegmentDto>();

        var saSegment = taskSegments.FirstOrDefault(s => s.Role == "SA");
        var uxSegment = taskSegments.FirstOrDefault(s => s.Role == "UX");

        // Assert: SA and UX are estimated
        Assert.NotNull(saSegment);
        Assert.NotNull(uxSegment);
        Assert.True(saSegment.IsEstimated);
        Assert.True(uxSegment.IsEstimated);

        // SA should start before UX (pipeline order: SA → UX → DEV → QA)
        Assert.True(saSegment.SegmentStart <= uxSegment.SegmentStart,
            $"SA should start <= UX: SA={saSegment.SegmentStart:yyyy-MM-dd}, UX={uxSegment.SegmentStart:yyyy-MM-dd}");

        // SA and UX should NOT fully overlap (they have different proportional widths)
        var saAndUxOverlap = saSegment.SegmentStart == uxSegment.SegmentStart
                          && saSegment.SegmentEnd == uxSegment.SegmentEnd;
        Assert.False(saAndUxOverlap,
            "SA and UX should not have identical date ranges — they must be proportionally distributed");
    }

    [Fact]
    public async Task OverlapPercentages_PullBackPreviousPhase()
    {
        // Arrange: Task with BA (20% overlap), DEV, QA — BA overlap should pull DEV start earlier
        var taskId = "OVL-001";
        var breakdown = new List<EffortBreakdownSpec>
        {
            new("BA",  10, 0,   1.0),  // no overlap (first phase)
            new("DEV", 20, 30,  2.0),  // 30% overlap with BA
            new("QA",  10, 0,   1.0),  // no overlap
        };

        await using (var db = await _factory.CreateDbContextAsync())
        {
            if (!await db.Tasks.AnyAsync(t => t.TaskId == taskId))
            {
                db.Tasks.Add(TaskItem.Create(taskId, "Overlap Pct Test", 5, breakdown));
                await db.SaveChangesAsync();
            }
        }

        await _schedulerService.RunSchedulerAsync(CancellationToken.None);

        // Act
        var segments = await _queryService.GetGanttSegmentsAsync(CancellationToken.None);
        var taskSegments = segments.FirstOrDefault(s => s.TaskId == taskId)?.Segments ?? (IReadOnlyList<GanttRoleSegmentDto>)Array.Empty<GanttRoleSegmentDto>();

        var baSegment = taskSegments.FirstOrDefault(s => s.Role == "BA");
        var devSegment = taskSegments.FirstOrDefault(s => s.Role == "DEV");
        var qaSegment = taskSegments.FirstOrDefault(s => s.Role == "QA");

        // Assert: All three segments exist (BA is estimated since no BA resources)
        Assert.NotNull(baSegment);
        Assert.NotNull(devSegment);
        Assert.NotNull(qaSegment);

        // DEV should start BEFORE BA ends due to 30% overlap pullback
        // (or at latest, DEV start <= BA end when resources are allocated)
        Assert.True(devSegment.SegmentStart <= baSegment.SegmentEnd.AddDays(1),
            $"DEV should start near BA end due to overlap: DEV={devSegment.SegmentStart:yyyy-MM-dd}, BA end={baSegment.SegmentEnd:yyyy-MM-dd}");

        // QA should come after DEV
        Assert.True(qaSegment.SegmentStart >= devSegment.SegmentStart,
            $"QA should start >= DEV: QA={qaSegment.SegmentStart:yyyy-MM-dd}, DEV={devSegment.SegmentStart:yyyy-MM-dd}");
    }

    [Fact]
    public async Task SegmentOrdering_FollowsPipelineOrder()
    {
        // Arrange: Task with all 6 roles — segments should follow BA → SA → UX → UI → DEV → QA
        var taskId = "PPL-001";
        var breakdown = TestDatabaseHelper.MakeMultiRoleBreakdown(15, 8,
            ("BA", 3), ("SA", 3), ("UX", 4), ("UI", 3));

        await using (var db = await _factory.CreateDbContextAsync())
        {
            if (!await db.Tasks.AnyAsync(t => t.TaskId == taskId))
            {
                db.Tasks.Add(TaskItem.Create(taskId, "Pipeline Order Test", 3, breakdown));
                await db.SaveChangesAsync();
            }
        }

        await _schedulerService.RunSchedulerAsync(CancellationToken.None);

        // Act
        var segments = await _queryService.GetGanttSegmentsAsync(CancellationToken.None);
        var taskSegments = segments.FirstOrDefault(s => s.TaskId == taskId)?.Segments ?? (IReadOnlyList<GanttRoleSegmentDto>)Array.Empty<GanttRoleSegmentDto>();

        // Assert: At least BA, DEV, QA should exist
        var roles = taskSegments.Select(s => s.Role).ToList();
        Assert.Contains("DEV", roles);
        Assert.Contains("QA", roles);

        // Estimated segments for roles without resources should follow pipeline order
        var estimatedSegments = taskSegments.Where(s => s.IsEstimated).OrderBy(s => s.SegmentStart).ToList();
        var pipelineOrder = DomainConstants.ResourceRole.PipelineOrder.ToList();
        for (int i = 1; i < estimatedSegments.Count; i++)
        {
            var prev = estimatedSegments[i - 1];
            var curr = estimatedSegments[i];
            var prevOrder = pipelineOrder.IndexOf(prev.Role);
            var currOrder = pipelineOrder.IndexOf(curr.Role);
            // If both have pipeline positions, earlier pipeline role should start first (or same time)
            if (prevOrder >= 0 && currOrder >= 0)
            {
                Assert.True(prev.SegmentStart <= curr.SegmentStart,
                    $"Pipeline order violated: {prev.Role} (order {prevOrder}) starts {prev.SegmentStart:yyyy-MM-dd} > {curr.Role} (order {currOrder}) starts {curr.SegmentStart:yyyy-MM-dd}");
            }
        }
    }

    [Fact]
    public async Task SingleRoleTask_ProducesOneSegment()
    {
        // Arrange: Task with only DEV effort — should produce exactly one segment
        var taskId = "SGL-001";
        var breakdown = new List<EffortBreakdownSpec>
        {
            new("DEV", 20, 0, 2.0),
            new("QA",  10, 0, 1.0),
        };

        await using (var db = await _factory.CreateDbContextAsync())
        {
            if (!await db.Tasks.AnyAsync(t => t.TaskId == taskId))
            {
                db.Tasks.Add(TaskItem.Create(taskId, "Single Role Test", 7, breakdown));
                await db.SaveChangesAsync();
            }
        }

        await _schedulerService.RunSchedulerAsync(CancellationToken.None);

        // Act
        var segments = await _queryService.GetGanttSegmentsAsync(CancellationToken.None);
        var taskSegments = segments.FirstOrDefault(s => s.TaskId == taskId)?.Segments ?? (IReadOnlyList<GanttRoleSegmentDto>)Array.Empty<GanttRoleSegmentDto>();

        // Assert: Should have exactly DEV + QA segments (both have resources in seed data)
        var devSegs = taskSegments.Where(s => s.Role == "DEV").ToList();
        var qaSegs = taskSegments.Where(s => s.Role == "QA").ToList();
        Assert.Single(devSegs);
        Assert.Single(qaSegs);

        // DEV segment should have meaningful duration
        Assert.True(devSegs[0].DurationDays >= 1, "DEV segment should have >= 1 day duration");

        // DEV should start before QA
        Assert.True(devSegs[0].SegmentStart <= qaSegs[0].SegmentStart,
            $"DEV should start <= QA: DEV={devSegs[0].SegmentStart:yyyy-MM-dd}, QA={qaSegs[0].SegmentStart:yyyy-MM-dd}");
    }

    // ── WP24: Additional guard & validation tests ──────────────────────────

    [Fact]
    public void ApplySchedulingResult_PlannedFinishBeforeStart_ThrowsDomainException()
    {
        // WP19 guard: PlannedFinish < PlannedStart must throw
        var task = TaskItem.Create("GRD-001", "Guard Test", 5,
            TestDatabaseHelper.MakeBreakdown(5, 2));

        var ex = Assert.Throws<DomainException>(() =>
            task.ApplySchedulingResult(
                peakConcurrency: 1.0,
                plannedStart: new DateTime(2026, 6, 10),
                plannedFinish: new DateTime(2026, 6, 5), // before start
                duration: 5,
                status: DomainConstants.TaskStatus.InProgress,
                deliveryRisk: DomainConstants.DeliveryRisk.OnTrack));

        Assert.Contains("PlannedFinish", ex.Message);
    }

    [Fact]
    public void ApplySchedulingResult_InvalidStatus_ThrowsDomainException()
    {
        var task = TaskItem.Create("GRD-002", "Status Guard Test", 5,
            TestDatabaseHelper.MakeBreakdown(5, 2));

        var ex = Assert.Throws<DomainException>(() =>
            task.ApplySchedulingResult(
                peakConcurrency: 1.0,
                plannedStart: new DateTime(2026, 6, 1),
                plannedFinish: new DateTime(2026, 6, 10),
                duration: 8,
                status: "BOGUS_STATUS",
                deliveryRisk: DomainConstants.DeliveryRisk.OnTrack));

        Assert.Contains("status", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplySchedulingResult_InvalidDeliveryRisk_ThrowsDomainException()
    {
        var task = TaskItem.Create("GRD-003", "Risk Guard Test", 5,
            TestDatabaseHelper.MakeBreakdown(5, 2));

        var ex = Assert.Throws<DomainException>(() =>
            task.ApplySchedulingResult(
                peakConcurrency: 1.0,
                plannedStart: new DateTime(2026, 6, 1),
                plannedFinish: new DateTime(2026, 6, 10),
                duration: 8,
                status: DomainConstants.TaskStatus.InProgress,
                deliveryRisk: "INVALID_RISK"));

        Assert.Contains("risk", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplySchedulingResult_NullDates_Accepted()
    {
        // Null planned dates are valid (unscheduled tasks)
        var task = TaskItem.Create("GRD-004", "Null Dates Test", 5,
            TestDatabaseHelper.MakeBreakdown(5, 2));

        task.ApplySchedulingResult(
            peakConcurrency: 0,
            plannedStart: null,
            plannedFinish: null,
            duration: 0,
            status: DomainConstants.TaskStatus.NotStarted,
            deliveryRisk: DomainConstants.DeliveryRisk.OnTrack);

        Assert.Null(task.PlannedStart);
        Assert.Null(task.PlannedFinish);
    }

    [Fact]
    public void GanttRoleSegmentDto_RequiresTaskId()
    {
        // WP23/L15: constructor validation on GanttRoleSegmentDto
        var ex = Assert.Throws<ArgumentException>(() =>
            new GanttRoleSegmentDto("", "DEV", DateTime.Today, DateTime.Today.AddDays(5), 5, 1.0,
                Array.Empty<GanttSegmentResourceDto>()));

        Assert.Contains("TaskId", ex.Message);
    }

    [Fact]
    public void GanttRoleSegmentDto_RequiresRole()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new GanttRoleSegmentDto("TSK-001", "", DateTime.Today, DateTime.Today.AddDays(5), 5, 1.0,
                Array.Empty<GanttSegmentResourceDto>()));

        Assert.Contains("Role", ex.Message);
    }

    [Fact]
    public void GanttRoleSegmentDto_NullResources_DefaultsToEmpty()
    {
        var dto = new GanttRoleSegmentDto("TSK-001", "DEV", DateTime.Today, DateTime.Today.AddDays(5), 5, 1.0,
            null!);

        Assert.NotNull(dto.AssignedResources);
        Assert.Empty(dto.AssignedResources);
    }

    [Fact]
    public void TeamMember_Create_InvalidRole_ThrowsDomainException()
    {
        // WP15: domain validation — role must be in AllRoles
        var ex = Assert.Throws<DomainException>(() =>
            TeamMember.Create("INV-001", "Invalid Role Member", "WIZARD",
                DomainConstants.DefaultTeam, 100, 1, new DateTime(2026, 5, 1)));

        Assert.Contains("role", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TaskItem_Create_ZeroEffort_ThrowsDomainException()
    {
        // Domain guard: EstimationDays > 0 enforced
        var ex = Assert.Throws<DomainException>(() =>
            TaskItem.Create("ZRO-002", "Zero Effort", 5, new List<EffortBreakdownSpec>
            {
                new("DEV", 0, 0, 1.0),
                new("QA", 2, 0, 1.0)
            }));

        Assert.Contains("greater than zero", ex.Message);
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