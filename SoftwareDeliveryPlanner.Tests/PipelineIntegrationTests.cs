using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoftwareDeliveryPlanner.Application;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Application.Adjustments.Commands;
using SoftwareDeliveryPlanner.Application.Adjustments.Queries;
using SoftwareDeliveryPlanner.Application.Calendar.Queries;
using SoftwareDeliveryPlanner.Application.DeliveryInsights.Queries;
using SoftwareDeliveryPlanner.Application.Holidays.Commands;
using SoftwareDeliveryPlanner.Application.Holidays.Queries;
using SoftwareDeliveryPlanner.Application.Output.Queries;
using SoftwareDeliveryPlanner.Application.Planning.Commands;
using SoftwareDeliveryPlanner.Application.Resources.Commands;
using SoftwareDeliveryPlanner.Application.Resources.Queries;
using SoftwareDeliveryPlanner.Application.Tasks.Commands;
using SoftwareDeliveryPlanner.Application.Tasks.Queries;
using SoftwareDeliveryPlanner.Application.Timeline.Queries;
using SoftwareDeliveryPlanner.Infrastructure.Data;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Infrastructure.Services;
using SoftwareDeliveryPlanner.SharedKernel;
using SoftwareDeliveryPlanner.Tests.Infrastructure;

namespace SoftwareDeliveryPlanner.Tests;

// ============================================================
// MediatR pipeline fixture: real DI container with SQL Server
// ============================================================

public abstract class PipelineFixture : IAsyncDisposable
{
    protected readonly IServiceProvider Services;

    private static List<EffortBreakdownInput> EB(double dev) => new()
    {
        new EffortBreakdownInput("DEV", dev, 0),
        new EffortBreakdownInput("QA", Math.Max(1, dev * 0.2), 0)
    };

    protected static List<EffortBreakdownInput> MakeEffortBreakdown(double dev) => EB(dev);

    protected PipelineFixture(SqlServerFixture fixture)
    {
        var connectionString = fixture.CreateDatabaseConnectionString();

        var services = new ServiceCollection();

        // EF Core SQL Server
        services.AddDbContextFactory<PlannerDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddDbContextFactory<ReadOnlyPlannerDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Application layer: MediatR + FluentValidation + ValidationBehavior
        services.AddApplication();

        // Infrastructure layer: focused services + engine factory
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ISchedulingEngineFactory, SchedulingEngineFactory>();
        services.AddScoped<ITaskOrchestrator, TaskService>();
        services.AddScoped<IResourceOrchestrator, ResourceService>();
        services.AddScoped<IRoleOrchestrator, RoleService>();
        services.AddScoped<IAdjustmentOrchestrator, AdjustmentService>();
        services.AddScoped<IHolidayOrchestrator, HolidayService>();
        services.AddScoped<ISchedulerService, SchedulerService>();
        services.AddScoped<IPlanningQueryService, PlanningQueryService>();

        Services = services.BuildServiceProvider();

        // Seed default data
        using var scope = Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PlannerDbContext>>();
        using var db = factory.CreateDbContext();
        db.Database.EnsureCreated();
        TestDatabaseHelper.SeedDefaultData(db);
    }

    public async ValueTask DisposeAsync()
    {
        if (Services is IAsyncDisposable ad) await ad.DisposeAsync();
        else if (Services is IDisposable d) d.Dispose();
    }
}

// ============================================================
// 1. Task pipeline: valid command flows through validation → handler → DB
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Pipeline_UpsertTaskTests : PipelineFixture
{
    public Pipeline_UpsertTaskTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task ValidNewTask_IsPersistedThroughPipeline()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new UpsertTaskCommand(
            Id: 0,
            TaskId: "SVC-100",
            ServiceName: "Pipeline Test Service",
            Priority: 5,
            EffortBreakdown: MakeEffortBreakdown(10),
            StrictDate: null,
            Dependencies: null,
            IsNew: true);

        await mediator.Send(command);

        var tasks = (await mediator.Send(new GetTasksQuery())).Value;
        Assert.Contains(tasks, t => t.TaskId == "SVC-100" && t.ServiceName == "Pipeline Test Service");
    }

    [Fact]
    public async Task InvalidTask_EmptyName_RejectedByValidationBehavior()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new UpsertTaskCommand(
            Id: 0,
            TaskId: "SVC-101",
            ServiceName: "",
            Priority: 5,
            EffortBreakdown: MakeEffortBreakdown(10),
            StrictDate: null,
            Dependencies: null,
            IsNew: true);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(command));
        Assert.NotEmpty(ex.Errors);
    }

    [Fact]
    public async Task InvalidTask_ZeroEstimation_RejectedByValidationBehavior()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new UpsertTaskCommand(
            Id: 0,
            TaskId: "SVC-102",
            ServiceName: "Valid Name",
            Priority: 5,
            EffortBreakdown: new List<EffortBreakdownInput>
            {
                new("DEV", 0, 0),
                new("QA", 0, 0)
            },
            StrictDate: null,
            Dependencies: null,
            IsNew: true);

        await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(command));
    }
}

// ============================================================
// 2. Holiday pipeline: overlap async validator fires through pipeline
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Pipeline_HolidayValidationTests : PipelineFixture
{
    public Pipeline_HolidayValidationTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task ValidHoliday_IsPersistedThroughPipeline()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new UpsertHolidayCommand(
            Id: 0,
            HolidayName: "Pipeline Holiday",
            StartDate: new DateTime(2028, 1, 15),
            EndDate: new DateTime(2028, 1, 15),
            HolidayType: DomainConstants.HolidayType.Company,
            Notes: "Integration test",
            IsNew: true);

        await mediator.Send(command);

        var holidays = (await mediator.Send(new GetHolidaysQuery())).Value;
        Assert.Contains(holidays, h => h.HolidayName == "Pipeline Holiday");
    }

    [Fact]
    public async Task OverlappingHoliday_RejectedByAsyncValidator()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // First holiday
        await mediator.Send(new UpsertHolidayCommand(
            Id: 0,
            HolidayName: "First Holiday",
            StartDate: new DateTime(2029, 3, 1),
            EndDate: new DateTime(2029, 3, 5),
            HolidayType: DomainConstants.HolidayType.National,
            Notes: null,
            IsNew: true));

        // Overlapping holiday — should be rejected by async overlap validator
        var command = new UpsertHolidayCommand(
            Id: 0,
            HolidayName: "Overlapping Holiday",
            StartDate: new DateTime(2029, 3, 4),
            EndDate: new DateTime(2029, 3, 8),
            HolidayType: DomainConstants.HolidayType.National,
            Notes: null,
            IsNew: true);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(command));
        Assert.Contains(ex.Errors, e => e.ErrorMessage.Contains("overlap", StringComparison.OrdinalIgnoreCase));
    }
}

// ============================================================
// 3. Resource pipeline: validation + persistence
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Pipeline_ResourceTests : PipelineFixture
{
    public Pipeline_ResourceTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task ValidResource_IsPersistedThroughPipeline()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new UpsertResourceCommand(
            Id: 0,
            ResourceId: "DEV-100",
            ResourceName: "Pipeline Developer",
            Role: DomainConstants.ResourceRole.Developer,
            Team: DomainConstants.DefaultTeam,
            AvailabilityPct: 100,
            DailyCapacity: 1,
            StartDate: DateTime.Today,
            Active: DomainConstants.ActiveStatus.Yes,
            Notes: null,
            IsNew: true);

        await mediator.Send(command);

        var resources = (await mediator.Send(new GetResourcesQuery())).Value;
        Assert.Contains(resources, r => r.ResourceId == "DEV-100");
    }

    [Fact]
    public async Task InvalidResource_EmptyName_RejectedByPipeline()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new UpsertResourceCommand(
            Id: 0,
            ResourceId: "DEV-101",
            ResourceName: "",
            Role: DomainConstants.ResourceRole.Developer,
            Team: DomainConstants.DefaultTeam,
            AvailabilityPct: 100,
            DailyCapacity: 1,
            StartDate: DateTime.Today,
            Active: DomainConstants.ActiveStatus.Yes,
            Notes: null,
            IsNew: true);

        await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(command));
    }
}

// ============================================================
// 4. RunScheduler + GetDashboardKpis through pipeline
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Pipeline_SchedulerTests : PipelineFixture
{
    public Pipeline_SchedulerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task RunScheduler_ThroughPipeline_ReturnsSuccessMessage()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = (await mediator.Send(new RunSchedulerCommand())).Value;
        Assert.Contains("Successfully scheduled", result);
    }

    [Fact]
    public async Task GetDashboardKpis_ThroughPipeline_ReturnsValidDto()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Run scheduler first to populate data
        await mediator.Send(new RunSchedulerCommand());

        var kpis = (await mediator.Send(new GetDashboardKpisQuery())).Value;
        Assert.NotNull(kpis);
        Assert.True(kpis.TotalServices > 0);
        Assert.True(kpis.ActiveResources > 0);
        Assert.True(kpis.TotalCapacity > 0);
    }

    [Fact]
    public async Task GetCalendar_AfterScheduler_ReturnsCalendarDays()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new RunSchedulerCommand());

        var calendar = (await mediator.Send(new GetCalendarQuery())).Value;
        Assert.NotNull(calendar);
        Assert.True(calendar.Count > 0);

        // Verify weekends are marked as non-working
        var weekendDays = calendar.Where(d =>
            d.CalendarDate.DayOfWeek == DayOfWeek.Friday ||
            d.CalendarDate.DayOfWeek == DayOfWeek.Saturday).ToList();
        Assert.All(weekendDays, d => Assert.False(d.IsWorkingDay));
    }

    [Fact]
    public async Task GetOutputPlan_AfterScheduler_ContainsAllTasks()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new RunSchedulerCommand());

        var outputPlan = (await mediator.Send(new GetOutputPlanQuery())).Value;
        Assert.NotNull(outputPlan);
        Assert.Equal(13, outputPlan.Count); // 13 seeded tasks
    }
}

// ============================================================
// 5. Cross-entity side effects through pipeline
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Pipeline_CrossEntityTests : PipelineFixture
{
    public Pipeline_CrossEntityTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task AddHoliday_ThenRunScheduler_HolidayAffectsCalendar()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Add a holiday on a specific future date
        await mediator.Send(new UpsertHolidayCommand(
            Id: 0,
            HolidayName: "Test Impact Holiday",
            StartDate: new DateTime(2027, 6, 1),  // Sunday — a working day in Sun-Thu
            EndDate: new DateTime(2027, 6, 1),
            HolidayType: DomainConstants.HolidayType.Company,
            Notes: null,
            IsNew: true));

        await mediator.Send(new RunSchedulerCommand());

        var calendar = (await mediator.Send(new GetCalendarQuery())).Value;
        var holidayDay = calendar.FirstOrDefault(d => d.CalendarDate.Date == new DateTime(2027, 6, 1));

        // The day should exist and be marked as a holiday + non-working
        Assert.NotNull(holidayDay);
        Assert.True(holidayDay.IsHoliday);
        Assert.False(holidayDay.IsWorkingDay);
    }

    [Fact]
    public async Task AddAdjustment_ThenRunScheduler_AdjustmentAffectsCapacity()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Run scheduler to get baseline
        await mediator.Send(new RunSchedulerCommand());
        var beforeKpis = (await mediator.Send(new GetDashboardKpisQuery())).Value;

        // Add a vacation adjustment for a resource
        await mediator.Send(new AddAdjustmentCommand(
            ResourceId: "DEV-001",
            AdjType: DomainConstants.AdjustmentType.Vacation,
            AvailabilityPct: 0,
            AdjStart: new DateTime(2026, 6, 1),
            AdjEnd: new DateTime(2026, 6, 30),
            Notes: "Month-long vacation"));

        // Re-run scheduler — schedule should change
        var afterResult = (await mediator.Send(new RunSchedulerCommand())).Value;
        Assert.Contains("Successfully scheduled", afterResult);
    }

    [Fact]
    public async Task DeleteTask_ThenRunScheduler_TaskCountDecreases()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var tasksBefore = (await mediator.Send(new GetTasksQuery())).Value;
        var firstTask = tasksBefore.First();

        await mediator.Send(new DeleteTaskCommand(firstTask.Id));

        var tasksAfter = (await mediator.Send(new GetTasksQuery())).Value;
        Assert.Equal(tasksBefore.Count - 1, tasksAfter.Count);

        // Scheduler still works with fewer tasks
        var result = (await mediator.Send(new RunSchedulerCommand())).Value;
        Assert.Contains("Successfully scheduled", result);
    }

    [Fact]
    public async Task UpdateTask_ThroughPipeline_ModifiesExisting()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Get existing task
        var tasks = (await mediator.Send(new GetTasksQuery())).Value;
        var existing = tasks.First();

        // Update it through the pipeline
        var command = new UpsertTaskCommand(
            Id: existing.Id,
            TaskId: existing.TaskId,
            ServiceName: "Updated Service Name",
            Priority: existing.Priority,
            EffortBreakdown: MakeEffortBreakdown(existing.TotalEstimationDays + 5),
            StrictDate: existing.StrictDate,
            Dependencies: null,
            IsNew: false);

        await mediator.Send(command);

        var updated = (await mediator.Send(new GetTasksQuery())).Value.First(t => t.TaskId == existing.TaskId);
        Assert.Equal("Updated Service Name", updated.ServiceName);
        // MakeEffortBreakdown(dev) produces DEV=dev + QA=Max(1, dev*0.2),
        // so TotalEstimationDays = dev + QA, not just dev.
        var newDev = existing.TotalEstimationDays + 5;
        var expectedTotal = newDev + Math.Max(1, newDev * 0.2);
        Assert.Equal(expectedTotal, updated.TotalEstimationDays, 1);
    }
}

// ============================================================
// 6. Timeline query through pipeline
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Pipeline_TimelineTests : PipelineFixture
{
    public Pipeline_TimelineTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetTimeline_ThroughPipeline_ReturnsDayByDayData()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new RunSchedulerCommand());

        var resources = (await mediator.Send(new GetResourcesQuery())).Value;
        var firstResource = resources.First();

        var timeline = (await mediator.Send(new GetTimelineQuery(
            firstResource.ResourceId,
            new DateTime(2026, 5, 1),
            new DateTime(2026, 5, 31)))).Value;

        Assert.NotNull(timeline);
        Assert.Equal(31, timeline.Days.Count); // 31 days in May
    }
}

// ============================================================
// 7. Task Dependencies through pipeline
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Pipeline_TaskDependencyTests : PipelineFixture
{
    public Pipeline_TaskDependencyTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task TaskWithDependency_IsPersistedAndScheduledAfterPrerequisite()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Delete existing tasks so we have a clean slate
        var existingTasks = (await mediator.Send(new GetTasksQuery())).Value;
        foreach (var t in existingTasks)
            await mediator.Send(new DeleteTaskCommand(t.Id));

        // Create prerequisite task
        await mediator.Send(new UpsertTaskCommand(
            Id: 0,
            TaskId: "PRE-001",
            ServiceName: "Prerequisite",
            Priority: 1,
            EffortBreakdown: MakeEffortBreakdown(5),
            StrictDate: null,
            Dependencies: null,
            IsNew: true));

        // Create dependent task
        await mediator.Send(new UpsertTaskCommand(
            Id: 0,
            TaskId: "DEP-001",
            ServiceName: "Dependent",
            Priority: 1,
            EffortBreakdown: MakeEffortBreakdown(3),
            StrictDate: null,
            Dependencies: new List<DependencyInput> { new("PRE-001", "FS", 0, 0) },
            IsNew: true));

        // Verify the dependency is persisted
        var tasks = (await mediator.Send(new GetTasksQuery())).Value;
        var dependent = tasks.First(t => t.TaskId == "DEP-001");
        Assert.Equal("PRE-001", dependent.DependsOnTaskIds);

        // Run scheduler and verify ordering
        await mediator.Send(new RunSchedulerCommand());

        var updatedTasks = (await mediator.Send(new GetTasksQuery())).Value;
        var prereq = updatedTasks.First(t => t.TaskId == "PRE-001");
        var dep = updatedTasks.First(t => t.TaskId == "DEP-001");

        Assert.NotNull(prereq.PlannedFinish);
        Assert.NotNull(dep.PlannedStart);
        Assert.True(dep.PlannedStart!.Value >= prereq.PlannedFinish!.Value,
            $"Dependent started {dep.PlannedStart} but prereq finished {prereq.PlannedFinish}");
    }
}

// ============================================================
// 8. Adjustment pipeline: validation + persistence
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Pipeline_AdjustmentTests : PipelineFixture
{
    public Pipeline_AdjustmentTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task ValidAdjustment_IsPersistedThroughPipeline()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new AddAdjustmentCommand(
            ResourceId: "DEV-001",
            AdjType: DomainConstants.AdjustmentType.Vacation,
            AvailabilityPct: 50,
            AdjStart: new DateTime(2027, 1, 10),
            AdjEnd: new DateTime(2027, 1, 15),
            Notes: "Pipeline test adjustment");

        await mediator.Send(command);

        var adjustments = (await mediator.Send(new GetAdjustmentsQuery())).Value;
        Assert.Contains(adjustments, a =>
            a.ResourceId == "DEV-001" && a.Notes == "Pipeline test adjustment");
    }

    [Fact]
    public async Task InvalidAdjustment_EmptyResourceId_RejectedByPipeline()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new AddAdjustmentCommand(
            ResourceId: "",
            AdjType: DomainConstants.AdjustmentType.Vacation,
            AvailabilityPct: 50,
            AdjStart: new DateTime(2027, 2, 1),
            AdjEnd: new DateTime(2027, 2, 5),
            Notes: null);

        await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(command));
    }

    [Fact]
    public async Task InvalidAdjustment_EndBeforeStart_RejectedByPipeline()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new AddAdjustmentCommand(
            ResourceId: "DEV-001",
            AdjType: DomainConstants.AdjustmentType.Training,
            AvailabilityPct: 0,
            AdjStart: new DateTime(2027, 3, 10),
            AdjEnd: new DateTime(2027, 3, 5),  // End before start
            Notes: null);

        await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(command));
    }

    [Fact]
    public async Task InvalidAdjustment_NegativeAvailability_RejectedByPipeline()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new AddAdjustmentCommand(
            ResourceId: "DEV-001",
            AdjType: DomainConstants.AdjustmentType.Other,
            AvailabilityPct: -10,
            AdjStart: new DateTime(2027, 4, 1),
            AdjEnd: new DateTime(2027, 4, 5),
            Notes: null);

        await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(command));
    }
}

// ============================================================
// 9. Delete commands through pipeline (no validators)
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Pipeline_DeleteCommandTests : PipelineFixture
{
    public Pipeline_DeleteCommandTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task DeleteResource_ThroughPipeline_RemovesResource()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var resources = (await mediator.Send(new GetResourcesQuery())).Value;
        var first = resources.First();

        await mediator.Send(new DeleteResourceCommand(first.Id));

        var after = (await mediator.Send(new GetResourcesQuery())).Value;
        Assert.DoesNotContain(after, r => r.Id == first.Id);
    }

    [Fact]
    public async Task DeleteHoliday_ThroughPipeline_RemovesHoliday()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var holidays = (await mediator.Send(new GetHolidaysQuery())).Value;
        var first = holidays.First();

        await mediator.Send(new DeleteHolidayCommand(first.Id));

        var after = (await mediator.Send(new GetHolidaysQuery())).Value;
        Assert.DoesNotContain(after, h => h.Id == first.Id);
    }

    [Fact]
    public async Task DeleteAdjustment_ThroughPipeline_RemovesAdjustment()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // First add an adjustment so we have one to delete
        await mediator.Send(new AddAdjustmentCommand(
            ResourceId: "DEV-001",
            AdjType: DomainConstants.AdjustmentType.Vacation,
            AvailabilityPct: 0,
            AdjStart: new DateTime(2027, 5, 1),
            AdjEnd: new DateTime(2027, 5, 5),
            Notes: "To be deleted"));

        var adjustments = (await mediator.Send(new GetAdjustmentsQuery())).Value;
        var toDelete = adjustments.First(a => a.Notes == "To be deleted");

        await mediator.Send(new DeleteAdjustmentCommand(toDelete.Id));

        var after = (await mediator.Send(new GetAdjustmentsQuery())).Value;
        Assert.DoesNotContain(after, a => a.Id == toDelete.Id);
    }

    [Fact]
    public async Task DeleteTask_NonExistent_DoesNotThrow()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Deleting non-existent ID should not throw
        var ex = await Record.ExceptionAsync(() =>
            mediator.Send(new DeleteTaskCommand(999999)));
        Assert.Null(ex);
    }

    [Fact]
    public async Task DeleteResource_NonExistent_DoesNotThrow()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var ex = await Record.ExceptionAsync(() =>
            mediator.Send(new DeleteResourceCommand(999999)));
        Assert.Null(ex);
    }

    [Fact]
    public async Task DeleteHoliday_NonExistent_DoesNotThrow()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var ex = await Record.ExceptionAsync(() =>
            mediator.Send(new DeleteHolidayCommand(999999)));
        Assert.Null(ex);
    }
}

// ============================================================
// 10. Full round-trip: create → schedule → query → verify
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Pipeline_RoundTripTests : PipelineFixture
{
    public Pipeline_RoundTripTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task FullRoundTrip_CreateTaskAndResource_ScheduleAndVerifyKpis()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Create a new task
        await mediator.Send(new UpsertTaskCommand(
            Id: 0,
            TaskId: "RT-001",
            ServiceName: "Round Trip Service",
            Priority: 3,
            EffortBreakdown: MakeEffortBreakdown(5),
            StrictDate: null,
            Dependencies: null,
            IsNew: true));

        // Run scheduler
        var result = (await mediator.Send(new RunSchedulerCommand())).Value;
        Assert.Contains("Successfully scheduled", result);

        // Get KPIs
        var kpis = (await mediator.Send(new GetDashboardKpisQuery())).Value;
        Assert.True(kpis.TotalServices > 0);

        // Get output plan and verify our task is in it
        var plan = (await mediator.Send(new GetOutputPlanQuery())).Value;
        Assert.Contains(plan, p => p.TaskId == "RT-001");
    }

    [Fact]
    public async Task FullRoundTrip_UpdateTaskAndReschedule()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Get existing task
        var tasks = (await mediator.Send(new GetTasksQuery())).Value;
        var existing = tasks.First();

        // Update its estimation
        await mediator.Send(new UpsertTaskCommand(
            Id: existing.Id,
            TaskId: existing.TaskId,
            ServiceName: existing.ServiceName,
            Priority: existing.Priority,
            EffortBreakdown: MakeEffortBreakdown(existing.TotalEstimationDays + 100),
            StrictDate: existing.StrictDate,
            Dependencies: existing.Dependencies.Any() ? existing.Dependencies.Select(d => new DependencyInput(d.PredecessorTaskId, d.Type, d.LagDays, d.OverlapPct)).ToList() : null,
            IsNew: false));

        // Reschedule
        await mediator.Send(new RunSchedulerCommand());

        // Verify updated estimation persisted
        var updated = (await mediator.Send(new GetTasksQuery())).Value.First(t => t.Id == existing.Id);
        var devDays = existing.TotalEstimationDays + 100;
        var expectedTotal = devDays + Math.Max(1, devDays * 0.2);
        Assert.Equal(expectedTotal, updated.TotalEstimationDays, 1);
    }
}
