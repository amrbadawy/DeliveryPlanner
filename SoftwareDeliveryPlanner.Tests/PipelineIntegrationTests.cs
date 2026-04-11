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
using SoftwareDeliveryPlanner.Data;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Infrastructure.Services;

namespace SoftwareDeliveryPlanner.Tests;

// ============================================================
// MediatR pipeline fixture: real DI container with in-memory DB
// ============================================================

public abstract class PipelineFixture : IAsyncDisposable
{
    protected readonly IServiceProvider Services;

    protected PipelineFixture()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();

        // EF Core in-memory
        services.AddDbContextFactory<PlannerDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        // Application layer: MediatR + FluentValidation + ValidationBehavior
        services.AddApplication();

        // Infrastructure layer: orchestrator + focused interface forwarding
        services.AddScoped<ISchedulingOrchestrator, SchedulingOrchestrator>();
        services.AddScoped<ISchedulerService>(sp => sp.GetRequiredService<ISchedulingOrchestrator>());
        services.AddScoped<ITaskOrchestrator>(sp => sp.GetRequiredService<ISchedulingOrchestrator>());
        services.AddScoped<IResourceOrchestrator>(sp => sp.GetRequiredService<ISchedulingOrchestrator>());
        services.AddScoped<IAdjustmentOrchestrator>(sp => sp.GetRequiredService<ISchedulingOrchestrator>());
        services.AddScoped<IHolidayOrchestrator>(sp => sp.GetRequiredService<ISchedulingOrchestrator>());
        services.AddScoped<IPlanningQueryService>(sp => sp.GetRequiredService<ISchedulingOrchestrator>());

        Services = services.BuildServiceProvider();

        // Seed default data
        using var scope = Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PlannerDbContext>>();
        using var db = factory.CreateDbContext();
        db.Database.EnsureCreated();
        db.InitializeDefaultData();
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

public class Pipeline_UpsertTaskTests : PipelineFixture
{
    [Fact]
    public async Task ValidNewTask_IsPersistedThroughPipeline()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new UpsertTaskCommand(
            Id: 0,
            TaskId: "SVC-100",
            ServiceName: "Pipeline Test Service",
            DevEstimation: 10,
            MaxDev: 2,
            Priority: 5,
            StrictDate: null,
            DependsOnTaskIds: null,
            IsNew: true);

        await mediator.Send(command);

        var tasks = await mediator.Send(new GetTasksQuery());
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
            DevEstimation: 10,
            MaxDev: 2,
            Priority: 5,
            StrictDate: null,
            DependsOnTaskIds: null,
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
            DevEstimation: 0,
            MaxDev: 2,
            Priority: 5,
            StrictDate: null,
            DependsOnTaskIds: null,
            IsNew: true);

        await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(command));
    }
}

// ============================================================
// 2. Holiday pipeline: overlap async validator fires through pipeline
// ============================================================

public class Pipeline_HolidayValidationTests : PipelineFixture
{
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

        var holidays = await mediator.Send(new GetHolidaysQuery());
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

public class Pipeline_ResourceTests : PipelineFixture
{
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

        var resources = await mediator.Send(new GetResourcesQuery());
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

public class Pipeline_SchedulerTests : PipelineFixture
{
    [Fact]
    public async Task RunScheduler_ThroughPipeline_ReturnsSuccessMessage()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new RunSchedulerCommand());
        Assert.Contains("Successfully scheduled", result);
    }

    [Fact]
    public async Task GetDashboardKpis_ThroughPipeline_ReturnsValidDto()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Run scheduler first to populate data
        await mediator.Send(new RunSchedulerCommand());

        var kpis = await mediator.Send(new GetDashboardKpisQuery());
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

        var calendar = await mediator.Send(new GetCalendarQuery());
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

        var outputPlan = await mediator.Send(new GetOutputPlanQuery());
        Assert.NotNull(outputPlan);
        Assert.Equal(13, outputPlan.Count); // 13 seeded tasks
    }
}

// ============================================================
// 5. Cross-entity side effects through pipeline
// ============================================================

public class Pipeline_CrossEntityTests : PipelineFixture
{
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

        var calendar = await mediator.Send(new GetCalendarQuery());
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
        var beforeKpis = await mediator.Send(new GetDashboardKpisQuery());

        // Add a vacation adjustment for a resource
        await mediator.Send(new AddAdjustmentCommand(
            ResourceId: "DEV-001",
            AdjType: DomainConstants.AdjustmentType.Vacation,
            AvailabilityPct: 0,
            AdjStart: new DateTime(2026, 6, 1),
            AdjEnd: new DateTime(2026, 6, 30),
            Notes: "Month-long vacation"));

        // Re-run scheduler — schedule should change
        var afterResult = await mediator.Send(new RunSchedulerCommand());
        Assert.Contains("Successfully scheduled", afterResult);
    }

    [Fact]
    public async Task DeleteTask_ThenRunScheduler_TaskCountDecreases()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var tasksBefore = await mediator.Send(new GetTasksQuery());
        var firstTask = tasksBefore.First();

        await mediator.Send(new DeleteTaskCommand(firstTask.Id));

        var tasksAfter = await mediator.Send(new GetTasksQuery());
        Assert.Equal(tasksBefore.Count - 1, tasksAfter.Count);

        // Scheduler still works with fewer tasks
        var result = await mediator.Send(new RunSchedulerCommand());
        Assert.Contains("Successfully scheduled", result);
    }

    [Fact]
    public async Task UpdateTask_ThroughPipeline_ModifiesExisting()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Get existing task
        var tasks = await mediator.Send(new GetTasksQuery());
        var existing = tasks.First();

        // Update it through the pipeline
        var command = new UpsertTaskCommand(
            Id: existing.Id,
            TaskId: existing.TaskId,
            ServiceName: "Updated Service Name",
            DevEstimation: existing.DevEstimation + 5,
            MaxDev: existing.MaxDev,
            Priority: existing.Priority,
            StrictDate: existing.StrictDate,
            DependsOnTaskIds: null,
            IsNew: false);

        await mediator.Send(command);

        var updated = (await mediator.Send(new GetTasksQuery())).First(t => t.TaskId == existing.TaskId);
        Assert.Equal("Updated Service Name", updated.ServiceName);
        Assert.Equal(existing.DevEstimation + 5, updated.DevEstimation);
    }
}

// ============================================================
// 6. Timeline query through pipeline
// ============================================================

public class Pipeline_TimelineTests : PipelineFixture
{
    [Fact]
    public async Task GetTimeline_ThroughPipeline_ReturnsDayByDayData()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new RunSchedulerCommand());

        var resources = await mediator.Send(new GetResourcesQuery());
        var firstResource = resources.First();

        var timeline = await mediator.Send(new GetTimelineQuery(
            firstResource.ResourceId,
            new DateTime(2026, 5, 1),
            new DateTime(2026, 5, 31)));

        Assert.NotNull(timeline);
        Assert.Equal(31, timeline.Days.Count); // 31 days in May
    }
}

// ============================================================
// 7. Task Dependencies through pipeline
// ============================================================

public class Pipeline_TaskDependencyTests : PipelineFixture
{
    [Fact]
    public async Task TaskWithDependency_IsPersistedAndScheduledAfterPrerequisite()
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Delete existing tasks so we have a clean slate
        var existingTasks = await mediator.Send(new GetTasksQuery());
        foreach (var t in existingTasks)
            await mediator.Send(new DeleteTaskCommand(t.Id));

        // Create prerequisite task
        await mediator.Send(new UpsertTaskCommand(
            Id: 0,
            TaskId: "PRE-001",
            ServiceName: "Prerequisite",
            DevEstimation: 5,
            MaxDev: 1,
            Priority: 1,
            StrictDate: null,
            DependsOnTaskIds: null,
            IsNew: true));

        // Create dependent task
        await mediator.Send(new UpsertTaskCommand(
            Id: 0,
            TaskId: "DEP-001",
            ServiceName: "Dependent",
            DevEstimation: 3,
            MaxDev: 1,
            Priority: 1,
            StrictDate: null,
            DependsOnTaskIds: "PRE-001",
            IsNew: true));

        // Verify the dependency is persisted
        var tasks = await mediator.Send(new GetTasksQuery());
        var dependent = tasks.First(t => t.TaskId == "DEP-001");
        Assert.Equal("PRE-001", dependent.DependsOnTaskIds);

        // Run scheduler and verify ordering
        await mediator.Send(new RunSchedulerCommand());

        var updatedTasks = await mediator.Send(new GetTasksQuery());
        var prereq = updatedTasks.First(t => t.TaskId == "PRE-001");
        var dep = updatedTasks.First(t => t.TaskId == "DEP-001");

        Assert.NotNull(prereq.PlannedFinish);
        Assert.NotNull(dep.PlannedStart);
        Assert.True(dep.PlannedStart!.Value >= prereq.PlannedFinish!.Value,
            $"Dependent started {dep.PlannedStart} but prereq finished {prereq.PlannedFinish}");
    }
}
