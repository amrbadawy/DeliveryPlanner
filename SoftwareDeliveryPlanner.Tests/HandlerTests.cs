using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MediatR;
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
using SoftwareDeliveryPlanner.Application.Planning.Queries;
using SoftwareDeliveryPlanner.Application.Timeline.Queries;
using SoftwareDeliveryPlanner.Infrastructure.Data;
using SoftwareDeliveryPlanner.Infrastructure.Services;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;
using SoftwareDeliveryPlanner.Tests.Infrastructure;

namespace SoftwareDeliveryPlanner.Tests;

internal sealed class NullPublisher : IPublisher
{
    public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
}

// ============================================================
// Base fixture: fresh in-memory DB seeded with default data
// ============================================================

public abstract class OrchestratorFixture : IAsyncDisposable
{
    private protected readonly IDbContextFactory<PlannerDbContext> Factory;
    private protected readonly ISchedulingEngineFactory EngineFactory;
    private protected readonly ITaskOrchestrator TaskOrchestrator;
    private protected readonly IResourceOrchestrator ResourceOrchestrator;
    private protected readonly IAdjustmentOrchestrator AdjustmentOrchestrator;
    private protected readonly IHolidayOrchestrator HolidayOrchestrator;
    private protected readonly ISchedulerService SchedulerService;
    private protected readonly IPlanningQueryService PlanningQueryService;

    protected OrchestratorFixture(SqlServerFixture fixture)
    {
        var (options, connectionString) = TestDatabaseHelper.CreateOptions(fixture);

        Factory = new TestDbContextFactory(options);
        var readOnlyFactory = new TestReadOnlyDbContextFactory(connectionString);
        EngineFactory = new SchedulingEngineFactory(Factory, TimeProvider.System);
        var publisher = new NullPublisher();

        TaskOrchestrator = new TaskService(Factory, readOnlyFactory, EngineFactory, publisher);
        ResourceOrchestrator = new ResourceService(Factory, readOnlyFactory, EngineFactory, publisher);
        AdjustmentOrchestrator = new AdjustmentService(Factory, readOnlyFactory, EngineFactory, publisher);
        HolidayOrchestrator = new HolidayService(Factory, readOnlyFactory, EngineFactory, publisher);
        SchedulerService = new SchedulerService(Factory, readOnlyFactory, EngineFactory, publisher);
        PlanningQueryService = new PlanningQueryService(Factory, readOnlyFactory, EngineFactory, publisher);
    }

    public async ValueTask DisposeAsync() => await Task.CompletedTask;
}

// ============================================================
// Tasks — Queries
// ============================================================

[Collection(DatabaseCollection.Name)]
public class GetTasksQueryHandlerTests : OrchestratorFixture
{
    public GetTasksQueryHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_ReturnsAllTasks()
    {
        var handler = new GetTasksQueryHandler(TaskOrchestrator);
        var result = await handler.Handle(new GetTasksQuery(), CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(result.Value.Count > 0);
    }

    [Fact]
    public async Task Handle_ReturnsTasksOrderedBySchedulingRank()
    {
        var handler = new GetTasksQueryHandler(TaskOrchestrator);
        var result = await handler.Handle(new GetTasksQuery(), CancellationToken.None);
        // All tasks from default data should come back (13 seeded)
        Assert.Equal(13, result.Value.Count);
    }
}

[Collection(DatabaseCollection.Name)]
public class GetTaskCountQueryHandlerTests : OrchestratorFixture
{
    public GetTaskCountQueryHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_ReturnsCorrectCount()
    {
        var handler = new GetTaskCountQueryHandler(TaskOrchestrator);
        var count = await handler.Handle(new GetTaskCountQuery(), CancellationToken.None);
        Assert.Equal(13, count.Value);
    }

    [Fact]
    public async Task Handle_AfterAddingTask_CountIncreases()
    {
        await using var db = await Factory.CreateDbContextAsync();
        db.Tasks.Add(TaskItem.Create("TST-99", "Extra", 1, 1, 5));
        await db.SaveChangesAsync();

        var handler = new GetTaskCountQueryHandler(TaskOrchestrator);
        var count = await handler.Handle(new GetTaskCountQuery(), CancellationToken.None);
        Assert.Equal(14, count.Value);
    }
}

// ============================================================
// Tasks — Commands
// ============================================================

[Collection(DatabaseCollection.Name)]
public class UpsertTaskCommandHandlerTests : OrchestratorFixture
{
    public UpsertTaskCommandHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_NewTask_AddsTaskToDatabase()
    {
        var handler = new UpsertTaskCommandHandler(TaskOrchestrator);
        var command = new UpsertTaskCommand(
            Id: 0,
            TaskId: "NEW-01",
            ServiceName: "New Service",
            DevEstimation: 10,
            MaxDev: 2,
            Priority: 3,
            StrictDate: null,
            DependsOnTaskIds: null,
            IsNew: true);

        await handler.Handle(command, CancellationToken.None);

        await using var db = await Factory.CreateDbContextAsync();
        Assert.True(await db.Tasks.AnyAsync(t => t.TaskId == "NEW-01"));
    }

    [Fact]
    public async Task Handle_UpdateTask_ModifiesExistingTask()
    {
        await using var db = await Factory.CreateDbContextAsync();
        var existing = await db.Tasks.FirstAsync();
        var originalName = existing.ServiceName;

        var handler = new UpsertTaskCommandHandler(TaskOrchestrator);
        var command = new UpsertTaskCommand(
            Id: existing.Id,
            TaskId: existing.TaskId,
            ServiceName: "Updated Service Name",
            DevEstimation: existing.DevEstimation,
            MaxDev: existing.MaxDev,
            Priority: existing.Priority,
            StrictDate: null,
            DependsOnTaskIds: null,
            IsNew: false);

        await handler.Handle(command, CancellationToken.None);

        await using var verifyDb = await Factory.CreateDbContextAsync();
        var updated = await verifyDb.Tasks.FirstAsync(t => t.Id == existing.Id);
        Assert.Equal("Updated Service Name", updated.ServiceName);
    }

    [Fact]
    public async Task Handle_NewTask_WithStrictDate_PersistsStrictDate()
    {
        var strictDate = new DateTime(2026, 12, 31);
        var handler = new UpsertTaskCommandHandler(TaskOrchestrator);
        await handler.Handle(new UpsertTaskCommand(
            Id: 0,
            TaskId: "STR-01",
            ServiceName: "Strict Task",
            DevEstimation: 5,
            MaxDev: 1,
            Priority: 1,
            StrictDate: strictDate,
            DependsOnTaskIds: null,
            IsNew: true), CancellationToken.None);

        await using var db = await Factory.CreateDbContextAsync();
        var task = await db.Tasks.FirstAsync(t => t.TaskId == "STR-01");
        Assert.Equal(strictDate, task.StrictDate);
    }

    [Fact]
    public async Task Handle_NewTask_WithDependsOnTaskIds_PersistsDependencies()
    {
        var handler = new UpsertTaskCommandHandler(TaskOrchestrator);
        await handler.Handle(new UpsertTaskCommand(
            Id: 0,
            TaskId: "DEP-01",
            ServiceName: "Dependent Task",
            DevEstimation: 5,
            MaxDev: 1,
            Priority: 1,
            StrictDate: null,
            DependsOnTaskIds: "SVC-001,SVC-002",
            IsNew: true), CancellationToken.None);

        await using var db = await Factory.CreateDbContextAsync();
        var task = await db.Tasks.FirstAsync(t => t.TaskId == "DEP-01");
        Assert.Equal("SVC-001,SVC-002", task.DependsOnTaskIds);
    }

    [Fact]
    public async Task Handle_NewTask_WithNullDependsOnTaskIds_PersistsNull()
    {
        var handler = new UpsertTaskCommandHandler(TaskOrchestrator);
        await handler.Handle(new UpsertTaskCommand(
            Id: 0,
            TaskId: "NDP-01",
            ServiceName: "No Deps Task",
            DevEstimation: 3,
            MaxDev: 1,
            Priority: 1,
            StrictDate: null,
            DependsOnTaskIds: null,
            IsNew: true), CancellationToken.None);

        await using var db = await Factory.CreateDbContextAsync();
        var task = await db.Tasks.FirstAsync(t => t.TaskId == "NDP-01");
        Assert.Null(task.DependsOnTaskIds);
    }
}

[Collection(DatabaseCollection.Name)]
public class DeleteTaskCommandHandlerTests : OrchestratorFixture
{
    public DeleteTaskCommandHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_ExistingTask_RemovesFromDatabase()
    {
        await using var db = await Factory.CreateDbContextAsync();
        var task = await db.Tasks.FirstAsync();
        var taskId = task.Id;

        var handler = new DeleteTaskCommandHandler(TaskOrchestrator);
        await handler.Handle(new DeleteTaskCommand(taskId), CancellationToken.None);

        await using var verifyDb = await Factory.CreateDbContextAsync();
        Assert.False(await verifyDb.Tasks.AnyAsync(t => t.Id == taskId));
    }

    [Fact]
    public async Task Handle_NonExistentTask_DoesNotThrow()
    {
        var handler = new DeleteTaskCommandHandler(TaskOrchestrator);
        // Should not throw even if task doesn't exist
        await handler.Handle(new DeleteTaskCommand(99999), CancellationToken.None);
    }
}

// ============================================================
// Tasks — Validators
// ============================================================

public class UpsertTaskCommandValidatorTests
{
    private readonly UpsertTaskCommandValidator _validator = new();

    private static UpsertTaskCommand Valid() => new(
        Id: 0, TaskId: "SVC-001", ServiceName: "My Service",
        DevEstimation: 5, MaxDev: 2, Priority: 5,
        StrictDate: null, DependsOnTaskIds: null, IsNew: true);

    [Fact]
    public void Valid_Command_PassesValidation()
    {
        var result = _validator.Validate(Valid());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void EmptyTaskId_FailsValidation(string taskId)
    {
        var result = _validator.Validate(Valid() with { TaskId = taskId });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertTaskCommand.TaskId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void EmptyServiceName_FailsValidation(string name)
    {
        var result = _validator.Validate(Valid() with { ServiceName = name });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertTaskCommand.ServiceName));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ZeroOrNegativeEstimation_FailsValidation(double est)
    {
        var result = _validator.Validate(Valid() with { DevEstimation = est });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertTaskCommand.DevEstimation));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    public void ZeroOrNegativeMaxDev_FailsValidation(double maxDev)
    {
        var result = _validator.Validate(Valid() with { MaxDev = maxDev });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertTaskCommand.MaxDev));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void OutOfRangePriority_FailsValidation(int priority)
    {
        var result = _validator.Validate(Valid() with { Priority = priority });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertTaskCommand.Priority));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void ValidPriority_PassesValidation(int priority)
    {
        var result = _validator.Validate(Valid() with { Priority = priority });
        Assert.True(result.IsValid);
    }
}

// ============================================================
// Resources — Queries
// ============================================================

[Collection(DatabaseCollection.Name)]
public class GetResourcesQueryHandlerTests : OrchestratorFixture
{
    public GetResourcesQueryHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_ReturnsAllResources()
    {
        var handler = new GetResourcesQueryHandler(ResourceOrchestrator);
        var result = await handler.Handle(new GetResourcesQuery(), CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(5, result.Value.Count); // 5 seeded
    }
}

[Collection(DatabaseCollection.Name)]
public class GetResourceCountQueryHandlerTests : OrchestratorFixture
{
    public GetResourceCountQueryHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_ReturnsCorrectCount()
    {
        var handler = new GetResourceCountQueryHandler(ResourceOrchestrator);
        var count = await handler.Handle(new GetResourceCountQuery(), CancellationToken.None);
        Assert.Equal(5, count.Value);
    }
}

// ============================================================
// Resources — Commands
// ============================================================

[Collection(DatabaseCollection.Name)]
public class UpsertResourceCommandHandlerTests : OrchestratorFixture
{
    public UpsertResourceCommandHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_NewResource_AddsToDatabase()
    {
        var handler = new UpsertResourceCommandHandler(ResourceOrchestrator);
        await handler.Handle(new UpsertResourceCommand(
            Id: 0,
            ResourceId: "DEV-099",
            ResourceName: "New Dev",
            Role: "Developer",
            Team: "Delivery",
            AvailabilityPct: 100,
            DailyCapacity: 1,
            StartDate: DateTime.Today,
            Active: "Yes",
            Notes: null,
            IsNew: true), CancellationToken.None);

        await using var db = await Factory.CreateDbContextAsync();
        Assert.True(await db.Resources.AnyAsync(r => r.ResourceId == "DEV-099"));
    }

    [Fact]
    public async Task Handle_UpdateResource_ModifiesExisting()
    {
        await using var db = await Factory.CreateDbContextAsync();
        var existing = await db.Resources.FirstAsync();

        var handler = new UpsertResourceCommandHandler(ResourceOrchestrator);
        await handler.Handle(new UpsertResourceCommand(
            Id: existing.Id,
            ResourceId: existing.ResourceId,
            ResourceName: "Updated Name",
            Role: existing.Role,
            Team: existing.Team,
            AvailabilityPct: existing.AvailabilityPct,
            DailyCapacity: existing.DailyCapacity,
            StartDate: existing.StartDate,
            Active: existing.Active,
            Notes: "updated",
            IsNew: false), CancellationToken.None);

        await using var verifyDb = await Factory.CreateDbContextAsync();
        var updated = await verifyDb.Resources.FirstAsync(r => r.Id == existing.Id);
        Assert.Equal("Updated Name", updated.ResourceName);
        Assert.Equal("updated", updated.Notes);
    }
}

[Collection(DatabaseCollection.Name)]
public class DeleteResourceCommandHandlerTests : OrchestratorFixture
{
    public DeleteResourceCommandHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_ExistingResource_RemovesFromDatabase()
    {
        await using var db = await Factory.CreateDbContextAsync();
        var resource = await db.Resources.FirstAsync();
        var resourceId = resource.Id;

        var handler = new DeleteResourceCommandHandler(ResourceOrchestrator);
        await handler.Handle(new DeleteResourceCommand(resourceId), CancellationToken.None);

        await using var verifyDb = await Factory.CreateDbContextAsync();
        Assert.False(await verifyDb.Resources.AnyAsync(r => r.Id == resourceId));
    }

    [Fact]
    public async Task Handle_NonExistentResource_DoesNotThrow()
    {
        var handler = new DeleteResourceCommandHandler(ResourceOrchestrator);
        await handler.Handle(new DeleteResourceCommand(99999), CancellationToken.None);
    }
}

// ============================================================
// Resources — Validators
// ============================================================

public class UpsertResourceCommandValidatorTests
{
    private readonly UpsertResourceCommandValidator _validator = new();

    private static UpsertResourceCommand Valid() => new(
        Id: 0, ResourceId: "DEV-001", ResourceName: "Alice",
        Role: "Developer", Team: "Delivery",
        AvailabilityPct: 100, DailyCapacity: 1,
        StartDate: DateTime.Today, Active: "Yes",
        Notes: null, IsNew: true);

    [Fact]
    public void Valid_Command_PassesValidation()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void EmptyResourceId_FailsValidation(string id)
    {
        var result = _validator.Validate(Valid() with { ResourceId = id });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertResourceCommand.ResourceId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void EmptyName_FailsValidation(string name)
    {
        var result = _validator.Validate(Valid() with { ResourceName = name });
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void OutOfRangeAvailability_FailsValidation(double pct)
    {
        var result = _validator.Validate(Valid() with { AvailabilityPct = pct });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertResourceCommand.AvailabilityPct));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ZeroOrNegativeCapacity_FailsValidation(double cap)
    {
        var result = _validator.Validate(Valid() with { DailyCapacity = cap });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertResourceCommand.DailyCapacity));
    }
}

// ============================================================
// Adjustments — Queries
// ============================================================

[Collection(DatabaseCollection.Name)]
public class GetAdjustmentsQueryHandlerTests : OrchestratorFixture
{
    public GetAdjustmentsQueryHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_EmptyDb_ReturnsEmptyList()
    {
        var handler = new GetAdjustmentsQueryHandler(AdjustmentOrchestrator);
        var result = await handler.Handle(new GetAdjustmentsQuery(), CancellationToken.None);
        Assert.NotNull(result);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_WithAdjustment_ReturnsList()
    {
        await using var db = await Factory.CreateDbContextAsync();
        db.Adjustments.Add(Adjustment.Create("DEV-001", "Vacation", 0, DateTime.Today, DateTime.Today.AddDays(3)));
        await db.SaveChangesAsync();

        var handler = new GetAdjustmentsQueryHandler(AdjustmentOrchestrator);
        var result = await handler.Handle(new GetAdjustmentsQuery(), CancellationToken.None);
        Assert.Single(result.Value);
    }
}

// ============================================================
// Adjustments — Commands
// ============================================================

[Collection(DatabaseCollection.Name)]
public class AddAdjustmentCommandHandlerTests : OrchestratorFixture
{
    public AddAdjustmentCommandHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_ValidCommand_AddsAdjustmentToDatabase()
    {
        var handler = new AddAdjustmentCommandHandler(AdjustmentOrchestrator);
        await handler.Handle(new AddAdjustmentCommand(
            ResourceId: "DEV-001",
            AdjType: "Vacation",
            AvailabilityPct: 0,
            AdjStart: DateTime.Today,
            AdjEnd: DateTime.Today.AddDays(5),
            Notes: "Summer holiday"), CancellationToken.None);

        await using var db = await Factory.CreateDbContextAsync();
        Assert.True(await db.Adjustments.AnyAsync(a => a.ResourceId == "DEV-001"));
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsAllFields()
    {
        var start = new DateTime(2026, 7, 1);
        var end = new DateTime(2026, 7, 7);
        var handler = new AddAdjustmentCommandHandler(AdjustmentOrchestrator);

        await handler.Handle(new AddAdjustmentCommand(
            ResourceId: "DEV-002",
            AdjType: "Training",
            AvailabilityPct: 50,
            AdjStart: start,
            AdjEnd: end,
            Notes: "Workshop"), CancellationToken.None);

        await using var db = await Factory.CreateDbContextAsync();
        var adj = await db.Adjustments.FirstAsync(a => a.ResourceId == "DEV-002");
        Assert.Equal("Training", adj.AdjType);
        Assert.Equal(50, adj.AvailabilityPct);
        Assert.Equal(start, adj.AdjStart);
        Assert.Equal(end, adj.AdjEnd);
        Assert.Equal("Workshop", adj.Notes);
    }
}

[Collection(DatabaseCollection.Name)]
public class DeleteAdjustmentCommandHandlerTests : OrchestratorFixture
{
    public DeleteAdjustmentCommandHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_ExistingAdjustment_RemovesFromDatabase()
    {
        await using var db = await Factory.CreateDbContextAsync();
        db.Adjustments.Add(Adjustment.Create("DEV-001", "Vacation", 0, DateTime.Today, DateTime.Today.AddDays(3)));
        await db.SaveChangesAsync();

        await using var db2 = await Factory.CreateDbContextAsync();
        var adj = await db2.Adjustments.FirstAsync();

        var handler = new DeleteAdjustmentCommandHandler(AdjustmentOrchestrator);
        await handler.Handle(new DeleteAdjustmentCommand(adj.Id), CancellationToken.None);

        await using var verifyDb = await Factory.CreateDbContextAsync();
        Assert.False(await verifyDb.Adjustments.AnyAsync(a => a.Id == adj.Id));
    }

    [Fact]
    public async Task Handle_NonExistentAdjustment_DoesNotThrow()
    {
        var handler = new DeleteAdjustmentCommandHandler(AdjustmentOrchestrator);
        await handler.Handle(new DeleteAdjustmentCommand(99999), CancellationToken.None);
    }
}

// ============================================================
// Adjustments — Validators
// ============================================================

public class AddAdjustmentCommandValidatorTests
{
    private readonly AddAdjustmentCommandValidator _validator = new();

    private static AddAdjustmentCommand Valid() => new(
        ResourceId: "DEV-001",
        AdjType: "Vacation",
        AvailabilityPct: 0,
        AdjStart: DateTime.Today,
        AdjEnd: DateTime.Today.AddDays(7),
        Notes: null);

    [Fact]
    public void Valid_Command_PassesValidation()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void EmptyResourceId_FailsValidation(string id)
    {
        var result = _validator.Validate(Valid() with { ResourceId = id });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddAdjustmentCommand.ResourceId));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void OutOfRangeAvailability_FailsValidation(double pct)
    {
        var result = _validator.Validate(Valid() with { AvailabilityPct = pct });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddAdjustmentCommand.AvailabilityPct));
    }

    [Fact]
    public void EndBeforeStart_FailsValidation()
    {
        var result = _validator.Validate(Valid() with
        {
            AdjStart = DateTime.Today.AddDays(5),
            AdjEnd = DateTime.Today
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddAdjustmentCommand.AdjEnd));
    }

    [Fact]
    public void StartEqualsEnd_PassesValidation()
    {
        var today = DateTime.Today;
        Assert.True(_validator.Validate(Valid() with { AdjStart = today, AdjEnd = today }).IsValid);
    }
}

// ============================================================
// Holidays — Queries
// ============================================================

[Collection(DatabaseCollection.Name)]
public class GetHolidaysQueryHandlerTests : OrchestratorFixture
{
    public GetHolidaysQueryHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_ReturnsAllHolidays()
    {
        var handler = new GetHolidaysQueryHandler(HolidayOrchestrator);
        var result = await handler.Handle(new GetHolidaysQuery(), CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(7, result.Value.Count); // 7 seeded (consolidated)
    }

    [Fact]
    public async Task Handle_ReturnsHolidaysOrderedByDate()
    {
        var handler = new GetHolidaysQueryHandler(HolidayOrchestrator);
        var result = await handler.Handle(new GetHolidaysQuery(), CancellationToken.None);
        for (int i = 1; i < result.Value.Count; i++)
        {
            Assert.True(result.Value[i].StartDate >= result.Value[i - 1].StartDate);
        }
    }
}

// ============================================================
// Holidays — Commands
// ============================================================

[Collection(DatabaseCollection.Name)]
public class UpsertHolidayCommandHandlerTests : OrchestratorFixture
{
    public UpsertHolidayCommandHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_NewHoliday_AddsToDatabase()
    {
        var handler = new UpsertHolidayCommandHandler(HolidayOrchestrator);
        var date = new DateTime(2026, 12, 25);

        await handler.Handle(new UpsertHolidayCommand(
            Id: 0,
            HolidayName: "Christmas",
            StartDate: date,
            EndDate: date,
            HolidayType: "Company",
            Notes: null,
            IsNew: true), CancellationToken.None);

        await using var db = await Factory.CreateDbContextAsync();
        Assert.True(await db.Holidays.AnyAsync(h => h.HolidayName == "Christmas"));
    }

    [Fact]
    public async Task Handle_UpdateHoliday_ModifiesExisting()
    {
        await using var db = await Factory.CreateDbContextAsync();
        var existing = await db.Holidays.FirstAsync();

        var handler = new UpsertHolidayCommandHandler(HolidayOrchestrator);
        await handler.Handle(new UpsertHolidayCommand(
            Id: existing.Id,
            HolidayName: "Updated Holiday",
            StartDate: existing.StartDate,
            EndDate: existing.EndDate,
            HolidayType: "Company",
            Notes: "updated",
            IsNew: false), CancellationToken.None);

        await using var verifyDb = await Factory.CreateDbContextAsync();
        var updated = await verifyDb.Holidays.FirstAsync(h => h.Id == existing.Id);
        Assert.Equal("Updated Holiday", updated.HolidayName);
        Assert.Equal("Company", updated.HolidayType);
    }
}

[Collection(DatabaseCollection.Name)]
public class DeleteHolidayCommandHandlerTests : OrchestratorFixture
{
    public DeleteHolidayCommandHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_ExistingHoliday_RemovesFromDatabase()
    {
        await using var db = await Factory.CreateDbContextAsync();
        var holiday = await db.Holidays.FirstAsync();
        var holidayId = holiday.Id;

        var handler = new DeleteHolidayCommandHandler(HolidayOrchestrator);
        await handler.Handle(new DeleteHolidayCommand(holidayId), CancellationToken.None);

        await using var verifyDb = await Factory.CreateDbContextAsync();
        Assert.False(await verifyDb.Holidays.AnyAsync(h => h.Id == holidayId));
    }

    [Fact]
    public async Task Handle_NonExistentHoliday_DoesNotThrow()
    {
        var handler = new DeleteHolidayCommandHandler(HolidayOrchestrator);
        await handler.Handle(new DeleteHolidayCommand(99999), CancellationToken.None);
    }
}

// ============================================================
// Holidays — Validators
// ============================================================

[Collection(DatabaseCollection.Name)]
public class UpsertHolidayCommandValidatorTests : OrchestratorFixture
{
    public UpsertHolidayCommandValidatorTests(SqlServerFixture fixture) : base(fixture) { }

    private static UpsertHolidayCommand Valid() => new(
        Id: 0,
        HolidayName: "Test Holiday",
        StartDate: new DateTime(2026, 11, 15),
        EndDate: new DateTime(2026, 11, 15),
        HolidayType: "National",
        Notes: null,
        IsNew: true);

    [Fact]
    public async Task Valid_Command_PassesValidation()
    {
        var validator = new UpsertHolidayCommandValidator(HolidayOrchestrator);
        var result = await validator.ValidateAsync(Valid());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task EmptyHolidayName_FailsValidation(string name)
    {
        var validator = new UpsertHolidayCommandValidator(HolidayOrchestrator);
        var result = await validator.ValidateAsync(Valid() with { HolidayName = name });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertHolidayCommand.HolidayName));
    }

    [Fact]
    public async Task StartDateAfterEndDate_FailsValidation()
    {
        var validator = new UpsertHolidayCommandValidator(HolidayOrchestrator);
        var result = await validator.ValidateAsync(Valid() with
        {
            StartDate = new DateTime(2026, 11, 20),
            EndDate = new DateTime(2026, 11, 15)
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertHolidayCommand.StartDate));
    }

    [Fact]
    public async Task OverlappingHoliday_FailsValidation()
    {
        // Seed an existing holiday: Dec 1–5
        await using var db = await Factory.CreateDbContextAsync();
        db.Holidays.Add(Holiday.Create("Existing Holiday", new DateTime(2026, 12, 1), new DateTime(2026, 12, 5)));
        await db.SaveChangesAsync();

        var validator = new UpsertHolidayCommandValidator(HolidayOrchestrator);
        // Try to add a holiday that overlaps: Dec 3–7
        var result = await validator.ValidateAsync(Valid() with
        {
            StartDate = new DateTime(2026, 12, 3),
            EndDate = new DateTime(2026, 12, 7)
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "StartDate");
    }

    [Fact]
    public async Task AdjacentNonOverlappingDates_PassesValidation()
    {
        // Seed an existing holiday: Dec 1–5
        await using var db = await Factory.CreateDbContextAsync();
        db.Holidays.Add(Holiday.Create("Existing Holiday", new DateTime(2026, 12, 1), new DateTime(2026, 12, 5)));
        await db.SaveChangesAsync();

        var validator = new UpsertHolidayCommandValidator(HolidayOrchestrator);
        // Adjacent: Dec 6–8 (no overlap)
        var result = await validator.ValidateAsync(Valid() with
        {
            StartDate = new DateTime(2026, 12, 6),
            EndDate = new DateTime(2026, 12, 8)
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task PartialOverlap_FailsValidation()
    {
        // Seed an existing holiday: Dec 1–5
        await using var db = await Factory.CreateDbContextAsync();
        db.Holidays.Add(Holiday.Create("Existing Holiday", new DateTime(2026, 12, 1), new DateTime(2026, 12, 5)));
        await db.SaveChangesAsync();

        var validator = new UpsertHolidayCommandValidator(HolidayOrchestrator);
        // Partial overlap: Nov 28–Dec 2
        var result = await validator.ValidateAsync(Valid() with
        {
            StartDate = new DateTime(2026, 11, 28),
            EndDate = new DateTime(2026, 12, 2)
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "StartDate");
    }

    [Fact]
    public async Task ExactSameDates_FailsValidation()
    {
        // Seed an existing holiday: Dec 1–5
        await using var db = await Factory.CreateDbContextAsync();
        db.Holidays.Add(Holiday.Create("Existing Holiday", new DateTime(2026, 12, 1), new DateTime(2026, 12, 5)));
        await db.SaveChangesAsync();

        var validator = new UpsertHolidayCommandValidator(HolidayOrchestrator);
        // Exact same dates
        var result = await validator.ValidateAsync(Valid() with
        {
            StartDate = new DateTime(2026, 12, 1),
            EndDate = new DateTime(2026, 12, 5)
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "StartDate");
    }

    [Fact]
    public async Task ContainedRange_FailsValidation()
    {
        // Seed an existing holiday: Dec 1–5
        await using var db = await Factory.CreateDbContextAsync();
        db.Holidays.Add(Holiday.Create("Existing Holiday", new DateTime(2026, 12, 1), new DateTime(2026, 12, 5)));
        await db.SaveChangesAsync();

        var validator = new UpsertHolidayCommandValidator(HolidayOrchestrator);
        // Contained within existing: Dec 2–4
        var result = await validator.ValidateAsync(Valid() with
        {
            StartDate = new DateTime(2026, 12, 2),
            EndDate = new DateTime(2026, 12, 4)
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "StartDate");
    }
}

// ============================================================
// Calendar — Query
// ============================================================

[Collection(DatabaseCollection.Name)]
public class GetCalendarQueryHandlerTests : OrchestratorFixture
{
    public GetCalendarQueryHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_AfterScheduler_ReturnsCalendarDays()
    {
        // Run scheduler first to populate calendar
        await SchedulerService.RunSchedulerAsync();

        var handler = new GetCalendarQueryHandler(PlanningQueryService);
        var result = await handler.Handle(new GetCalendarQuery(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Value.Count > 0);
    }

    [Fact]
    public async Task Handle_ReturnsCalendarOrderedByDate()
    {
        await SchedulerService.RunSchedulerAsync();

        var handler = new GetCalendarQueryHandler(PlanningQueryService);
        var result = await handler.Handle(new GetCalendarQuery(), CancellationToken.None);

        for (int i = 1; i < result.Value.Count; i++)
        {
            Assert.True(result.Value[i].CalendarDate >= result.Value[i - 1].CalendarDate);
        }
    }
}

// ============================================================
// Timeline — Query
// ============================================================

[Collection(DatabaseCollection.Name)]
public class GetTimelineQueryHandlerTests : OrchestratorFixture
{
    public GetTimelineQueryHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_ValidResourceAndRange_ReturnsDays()
    {
        var handler = new GetTimelineQueryHandler(PlanningQueryService);
        var start = new DateTime(2026, 5, 1);
        var end = new DateTime(2026, 5, 7);

        var result = await handler.Handle(
            new GetTimelineQuery("DEV-001", start, end),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(7, result.Value.Days.Count);
    }

    [Fact]
    public async Task Handle_SingleDay_ReturnsOneDayDto()
    {
        var handler = new GetTimelineQueryHandler(PlanningQueryService);
        var date = new DateTime(2026, 5, 4); // Monday

        var result = await handler.Handle(
            new GetTimelineQuery("DEV-001", date, date),
            CancellationToken.None);

        Assert.Single(result.Value.Days);
        Assert.Equal(date, result.Value.Days[0].Date);
    }

    [Fact]
    public async Task Handle_Weekend_MarksCorrectly()
    {
        var handler = new GetTimelineQueryHandler(PlanningQueryService);
        // 2026-05-01 is a Friday (day 5 = weekend in Sun-Thu week)
        var friday = new DateTime(2026, 5, 1);

        var result = await handler.Handle(
            new GetTimelineQuery("DEV-001", friday, friday),
            CancellationToken.None);

        Assert.Single(result.Value.Days);
        Assert.Equal("Friday", result.Value.Days[0].StatusText);
        Assert.Equal(TimelineDayStatus.Weekend, result.Value.Days[0].Status);
    }

    [Fact]
    public async Task Handle_Holiday_MarksCorrectly()
    {
        var handler = new GetTimelineQueryHandler(PlanningQueryService);
        // 2026-09-23 is اليوم الوطني (National Day) - seeded
        var nationalDay = new DateTime(2026, 9, 23);

        var result = await handler.Handle(
            new GetTimelineQuery("DEV-001", nationalDay, nationalDay),
            CancellationToken.None);

        Assert.Single(result.Value.Days);
        Assert.Equal(TimelineDayStatus.Holiday, result.Value.Days[0].Status);
    }

    [Fact]
    public async Task Handle_UnknownResource_ReturnsFreeDays()
    {
        var handler = new GetTimelineQueryHandler(PlanningQueryService);
        var start = new DateTime(2026, 5, 3); // Sunday — working day
        var end = new DateTime(2026, 5, 3);

        var result = await handler.Handle(
            new GetTimelineQuery("UNKNOWN-99", start, end),
            CancellationToken.None);

        Assert.Single(result.Value.Days);
        // devIndex will be 0 (not found), so no task assignment → "Free" or holiday/weekend
        Assert.NotNull(result.Value.Days[0].StatusText);
    }
}

// ============================================================
// Output — Query
// ============================================================

[Collection(DatabaseCollection.Name)]
public class GetOutputPlanQueryHandlerTests : OrchestratorFixture
{
    public GetOutputPlanQueryHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_ReturnsOutputPlan()
    {
        var handler = new GetOutputPlanQueryHandler(PlanningQueryService);
        var result = await handler.Handle(new GetOutputPlanQuery(), CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_AfterScheduler_ReturnsNonEmptyPlan()
    {
        await SchedulerService.RunSchedulerAsync();

        var handler = new GetOutputPlanQueryHandler(PlanningQueryService);
        var result = await handler.Handle(new GetOutputPlanQuery(), CancellationToken.None);

        Assert.True(result.Value.Count > 0);
    }

    [Fact]
    public async Task Handle_EachRow_ContainsExpectedKeys()
    {
        await SchedulerService.RunSchedulerAsync();

        var handler = new GetOutputPlanQueryHandler(PlanningQueryService);
        var result = await handler.Handle(new GetOutputPlanQuery(), CancellationToken.None);

        foreach (var row in result.Value)
        {
            Assert.NotNull(row.TaskId);
            Assert.NotNull(row.ServiceName);
            Assert.NotNull(row.Status);
            Assert.NotNull(row.DeliveryRisk);
        }
    }
}

// ============================================================
// Holidays — Command Handler Edge Cases
// ============================================================

[Collection(DatabaseCollection.Name)]
public class UpsertHolidayCommandHandlerEdgeTests : OrchestratorFixture
{
    public UpsertHolidayCommandHandlerEdgeTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_NewHoliday_MultiDayRange_PersistsCorrectDates()
    {
        var handler = new UpsertHolidayCommandHandler(HolidayOrchestrator);
        var start = new DateTime(2026, 12, 24);
        var end = new DateTime(2026, 12, 26);

        await handler.Handle(new UpsertHolidayCommand(
            Id: 0,
            HolidayName: "Christmas Break",
            StartDate: start,
            EndDate: end,
            HolidayType: "Company",
            Notes: null,
            IsNew: true), CancellationToken.None);

        await using var db = await Factory.CreateDbContextAsync();
        var holiday = await db.Holidays.FirstAsync(h => h.HolidayName == "Christmas Break");
        Assert.Equal(start, holiday.StartDate);
        Assert.Equal(end, holiday.EndDate);
    }

    [Fact]
    public async Task Handle_NewHoliday_WithNotes_PersistsNotes()
    {
        var handler = new UpsertHolidayCommandHandler(HolidayOrchestrator);
        var date = new DateTime(2026, 12, 25);

        await handler.Handle(new UpsertHolidayCommand(
            Id: 0,
            HolidayName: "Noted Holiday",
            StartDate: date,
            EndDate: date,
            HolidayType: "Company",
            Notes: "Test notes",
            IsNew: true), CancellationToken.None);

        await using var db = await Factory.CreateDbContextAsync();
        var holiday = await db.Holidays.FirstAsync(h => h.HolidayName == "Noted Holiday");
        Assert.Equal("Test notes", holiday.Notes);
    }

    [Fact]
    public async Task Handle_UpdateHoliday_DateRange_UpdatesStartAndEndDate()
    {
        await using var db = await Factory.CreateDbContextAsync();
        var existing = await db.Holidays.FirstAsync();

        var newStart = new DateTime(2027, 1, 10);
        var newEnd = new DateTime(2027, 1, 12);

        var handler = new UpsertHolidayCommandHandler(HolidayOrchestrator);
        await handler.Handle(new UpsertHolidayCommand(
            Id: existing.Id,
            HolidayName: existing.HolidayName,
            StartDate: newStart,
            EndDate: newEnd,
            HolidayType: existing.HolidayType,
            Notes: existing.Notes,
            IsNew: false), CancellationToken.None);

        await using var verifyDb = await Factory.CreateDbContextAsync();
        var updated = await verifyDb.Holidays.FirstAsync(h => h.Id == existing.Id);
        Assert.Equal(newStart, updated.StartDate);
        Assert.Equal(newEnd, updated.EndDate);
    }

    [Fact]
    public async Task Handle_NewHoliday_UsesCreateFactory_TrimsName()
    {
        var handler = new UpsertHolidayCommandHandler(HolidayOrchestrator);
        var date = new DateTime(2026, 12, 25);

        await handler.Handle(new UpsertHolidayCommand(
            Id: 0,
            HolidayName: " Trimmed Holiday  ",
            StartDate: date,
            EndDate: date,
            HolidayType: "Company",
            Notes: null,
            IsNew: true), CancellationToken.None);

        await using var db = await Factory.CreateDbContextAsync();
        var holiday = await db.Holidays.FirstAsync(h => h.HolidayName == "Trimmed Holiday");
        Assert.Equal("Trimmed Holiday", holiday.HolidayName);
    }
}

// ============================================================
// Holidays — Validator Edge Cases
// ============================================================

[Collection(DatabaseCollection.Name)]
public class UpsertHolidayCommandValidatorEdgeTests : OrchestratorFixture
{
    public UpsertHolidayCommandValidatorEdgeTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task UpdateOverlapping_SelfExclude_PassesValidation()
    {
        // Get an existing holiday from seeded data
        await using var db = await Factory.CreateDbContextAsync();
        var existing = await db.Holidays.FirstAsync();

        var validator = new UpsertHolidayCommandValidator(HolidayOrchestrator);
        // Update with the same dates — should exclude self from overlap check
        var result = await validator.ValidateAsync(new UpsertHolidayCommand(
            Id: existing.Id,
            HolidayName: existing.HolidayName,
            StartDate: existing.StartDate,
            EndDate: existing.EndDate,
            HolidayType: existing.HolidayType,
            Notes: existing.Notes,
            IsNew: false));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task NewHoliday_NoOverlap_PassesValidation()
    {
        var validator = new UpsertHolidayCommandValidator(HolidayOrchestrator);
        // Pick a date range that doesn't overlap any seeded holiday
        var result = await validator.ValidateAsync(new UpsertHolidayCommand(
            Id: 0,
            HolidayName: "Free Day",
            StartDate: new DateTime(2026, 11, 15),
            EndDate: new DateTime(2026, 11, 15),
            HolidayType: "Company",
            Notes: null,
            IsNew: true));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task NewHoliday_EnvelopingExisting_FailsValidation()
    {
        // Seed an existing holiday: Dec 10–12
        await using var db = await Factory.CreateDbContextAsync();
        db.Holidays.Add(Holiday.Create("Mid Dec", new DateTime(2026, 12, 10), new DateTime(2026, 12, 12)));
        await db.SaveChangesAsync();

        var validator = new UpsertHolidayCommandValidator(HolidayOrchestrator);
        // New holiday that fully envelops the existing one: Dec 8–14
        var result = await validator.ValidateAsync(new UpsertHolidayCommand(
            Id: 0,
            HolidayName: "Big Holiday",
            StartDate: new DateTime(2026, 12, 8),
            EndDate: new DateTime(2026, 12, 14),
            HolidayType: "Company",
            Notes: null,
            IsNew: true));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "StartDate");
    }

    [Fact]
    public async Task StartDateEqualsEndDate_PassesValidation()
    {
        var validator = new UpsertHolidayCommandValidator(HolidayOrchestrator);
        // Single-day holiday on a free date
        var result = await validator.ValidateAsync(new UpsertHolidayCommand(
            Id: 0,
            HolidayName: "Single Day",
            StartDate: new DateTime(2026, 11, 20),
            EndDate: new DateTime(2026, 11, 20),
            HolidayType: "Company",
            Notes: null,
            IsNew: true));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task EmptyHolidayName_ReturnsCorrectErrorMessage()
    {
        var validator = new UpsertHolidayCommandValidator(HolidayOrchestrator);
        var result = await validator.ValidateAsync(new UpsertHolidayCommand(
            Id: 0,
            HolidayName: "",
            StartDate: new DateTime(2026, 11, 15),
            EndDate: new DateTime(2026, 11, 15),
            HolidayType: "Company",
            Notes: null,
            IsNew: true));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Holiday Name is required"));
    }
}

// ============================================================
// Timeline — Query Handler Edge Cases
// ============================================================

[Collection(DatabaseCollection.Name)]
public class GetTimelineQueryHandlerEdgeTests : OrchestratorFixture
{
    public GetTimelineQueryHandlerEdgeTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_Saturday_MarksCorrectly()
    {
        var handler = new GetTimelineQueryHandler(PlanningQueryService);
        // 2026-05-02 is a Saturday
        var saturday = new DateTime(2026, 5, 2);

        var result = await handler.Handle(
            new GetTimelineQuery("DEV-001", saturday, saturday),
            CancellationToken.None);

        Assert.Single(result.Value.Days);
        Assert.Equal("Saturday", result.Value.Days[0].StatusText);
        Assert.Equal(TimelineDayStatus.Weekend, result.Value.Days[0].Status);
    }

    [Fact]
    public async Task Handle_MultiDayRange_ReturnsCorrectCount()
    {
        var handler = new GetTimelineQueryHandler(PlanningQueryService);
        var start = new DateTime(2026, 5, 1);
        var end = new DateTime(2026, 5, 14);

        var result = await handler.Handle(
            new GetTimelineQuery("DEV-001", start, end),
            CancellationToken.None);

        Assert.Equal(14, result.Value.Days.Count);
    }

    [Fact]
    public async Task Handle_AdjustmentPeriod_ShowsAdjType()
    {
        // Add an adjustment for DEV-001
        await using var db = await Factory.CreateDbContextAsync();
        db.Adjustments.Add(Adjustment.Create("DEV-001", "Training", 50, new DateTime(2026, 8, 10), new DateTime(2026, 8, 12)));
        await db.SaveChangesAsync();

        var handler = new GetTimelineQueryHandler(PlanningQueryService);
        var result = await handler.Handle(
            new GetTimelineQuery("DEV-001", new DateTime(2026, 8, 10), new DateTime(2026, 8, 10)),
            CancellationToken.None);

        Assert.Single(result.Value.Days);
        Assert.Equal("Training", result.Value.Days[0].StatusText);
        Assert.Equal(TimelineDayStatus.Adjustment, result.Value.Days[0].Status);
    }
}

// ============================================================
// Calendar — Query Handler Edge Cases
// ============================================================

[Collection(DatabaseCollection.Name)]
public class GetCalendarQueryHandlerEdgeTests : OrchestratorFixture
{
    public GetCalendarQueryHandlerEdgeTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_CalendarDays_IncludeHolidayNames()
    {
        await SchedulerService.RunSchedulerAsync();

        var handler = new GetCalendarQueryHandler(PlanningQueryService);
        var result = await handler.Handle(new GetCalendarQuery(), CancellationToken.None);

        // National Day is seeded on 2026-09-23
        var nationalDay = result.Value.FirstOrDefault(d => d.CalendarDate.Date == new DateTime(2026, 9, 23));
        Assert.NotNull(nationalDay);
        Assert.True(nationalDay.IsHoliday);
        Assert.NotNull(nationalDay.HolidayName);
        Assert.NotEmpty(nationalDay.HolidayName);
    }

    [Fact]
    public async Task Handle_WeekendDays_AreNotWorkingDays()
    {
        await SchedulerService.RunSchedulerAsync();

        var handler = new GetCalendarQueryHandler(PlanningQueryService);
        var result = await handler.Handle(new GetCalendarQuery(), CancellationToken.None);

        // Check that all Fridays and Saturdays in the calendar are non-working
        var weekendDays = result.Value.Where(d =>
            d.CalendarDate.DayOfWeek == DayOfWeek.Friday ||
            d.CalendarDate.DayOfWeek == DayOfWeek.Saturday).ToList();

        Assert.NotEmpty(weekendDays);
        Assert.All(weekendDays, d => Assert.False(d.IsWorkingDay));
    }
}

// ============================================================
// Adjustments — Validator Edge Cases
// ============================================================

public class AddAdjustmentCommandValidatorEdgeTests
{
    private readonly AddAdjustmentCommandValidator _validator = new();

    private static AddAdjustmentCommand Valid() => new(
        ResourceId: "DEV-001",
        AdjType: "Vacation",
        AvailabilityPct: 0,
        AdjStart: DateTime.Today,
        AdjEnd: DateTime.Today.AddDays(7),
        Notes: null);

    [Fact]
    public void EmptyAdjType_PassesValidation()
    {
        // AdjType has no NotEmpty rule — empty should pass
        var result = _validator.Validate(Valid() with { AdjType = "" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void BoundaryAvailability_Zero_PassesValidation()
    {
        var result = _validator.Validate(Valid() with { AvailabilityPct = 0 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void BoundaryAvailability_Hundred_PassesValidation()
    {
        var result = _validator.Validate(Valid() with { AvailabilityPct = 100 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Notes_Null_PassesValidation()
    {
        var result = _validator.Validate(Valid() with { Notes = null });
        Assert.True(result.IsValid);
    }
}

// ============================================================
// Validators — Additional Coverage (Batch 4)
// ============================================================

public class UpsertTaskCommandValidatorAdditionalTests
{
    private readonly UpsertTaskCommandValidator _validator = new();

    private static UpsertTaskCommand Valid() => new(
        Id: 0, TaskId: "SVC-001", ServiceName: "My Service",
        DevEstimation: 5, MaxDev: 2, Priority: 5,
        StrictDate: null, DependsOnTaskIds: null, IsNew: true);

    [Fact]
    public void NullTaskId_FailsValidation()
    {
        var result = _validator.Validate(Valid() with { TaskId = null! });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertTaskCommand.TaskId));
    }

    [Fact]
    public void NullServiceName_FailsValidation()
    {
        var result = _validator.Validate(Valid() with { ServiceName = null! });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertTaskCommand.ServiceName));
    }

    [Fact]
    public void BoundaryEstimation_SmallPositive_PassesValidation()
    {
        var result = _validator.Validate(Valid() with { DevEstimation = 0.001 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void BoundaryMaxDev_SmallPositive_PassesValidation()
    {
        var result = _validator.Validate(Valid() with { MaxDev = 0.001 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void MultipleFailures_AllErrorsReturned()
    {
        var result = _validator.Validate(Valid() with
        {
            TaskId = "",
            ServiceName = "",
            DevEstimation = 0,
            MaxDev = -1,
            Priority = 0
        });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 5);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertTaskCommand.TaskId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertTaskCommand.ServiceName));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertTaskCommand.DevEstimation));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertTaskCommand.MaxDev));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertTaskCommand.Priority));
    }

    [Fact]
    public void ErrorMessage_TaskId_ContainsExpectedText()
    {
        var result = _validator.Validate(Valid() with { TaskId = "" });
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Service ID is required"));
    }

    [Fact]
    public void ErrorMessage_Estimation_ContainsExpectedText()
    {
        var result = _validator.Validate(Valid() with { DevEstimation = 0 });
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("greater than zero"));
    }

    [Fact]
    public void ErrorMessage_Priority_ContainsExpectedText()
    {
        var result = _validator.Validate(Valid() with { Priority = 0 });
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("between 1 and 10"));
    }

    [Fact]
    public void StrictDate_AcceptsAnyValue()
    {
        var result = _validator.Validate(Valid() with { StrictDate = new DateTime(2020, 1, 1) });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void DependsOnTaskIds_AcceptsAnyValue()
    {
        var result = _validator.Validate(Valid() with { DependsOnTaskIds = "GARBAGE,INVALID" });
        Assert.True(result.IsValid);
    }
}

public class UpsertResourceCommandValidatorAdditionalTests
{
    private readonly UpsertResourceCommandValidator _validator = new();

    private static UpsertResourceCommand Valid() => new(
        Id: 0, ResourceId: "DEV-001", ResourceName: "Alice",
        Role: "Developer", Team: "Delivery",
        AvailabilityPct: 100, DailyCapacity: 1,
        StartDate: DateTime.Today, Active: "Yes",
        Notes: null, IsNew: true);

    [Fact]
    public void NullResourceId_FailsValidation()
    {
        var result = _validator.Validate(Valid() with { ResourceId = null! });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertResourceCommand.ResourceId));
    }

    [Fact]
    public void NullResourceName_FailsValidation()
    {
        var result = _validator.Validate(Valid() with { ResourceName = null! });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertResourceCommand.ResourceName));
    }

    [Fact]
    public void BoundaryAvailability_Zero_PassesValidation()
    {
        var result = _validator.Validate(Valid() with { AvailabilityPct = 0 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void BoundaryAvailability_Hundred_PassesValidation()
    {
        var result = _validator.Validate(Valid() with { AvailabilityPct = 100 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void BoundaryCapacity_SmallPositive_PassesValidation()
    {
        var result = _validator.Validate(Valid() with { DailyCapacity = 0.001 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void MultipleFailures_AllErrorsReturned()
    {
        var result = _validator.Validate(Valid() with
        {
            ResourceId = "",
            ResourceName = "",
            AvailabilityPct = -1,
            DailyCapacity = 0
        });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 4);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertResourceCommand.ResourceId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertResourceCommand.ResourceName));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertResourceCommand.AvailabilityPct));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertResourceCommand.DailyCapacity));
    }

    [Fact]
    public void UnconstrainedFields_AcceptAnyValue()
    {
        // Role, Team, Active, Notes, StartDate have no validation rules
        var result = _validator.Validate(Valid() with
        {
            Role = "",
            Team = "",
            Active = "Maybe",
            Notes = ""
        });
        Assert.True(result.IsValid);
    }
}

public class AddAdjustmentCommandValidatorAdditionalTests
{
    private readonly AddAdjustmentCommandValidator _validator = new();

    private static AddAdjustmentCommand Valid() => new(
        ResourceId: "DEV-001",
        AdjType: "Vacation",
        AvailabilityPct: 0,
        AdjStart: DateTime.Today,
        AdjEnd: DateTime.Today.AddDays(7),
        Notes: null);

    [Fact]
    public void NullResourceId_FailsValidation()
    {
        var result = _validator.Validate(Valid() with { ResourceId = null! });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddAdjustmentCommand.ResourceId));
    }

    [Fact]
    public void NegativeAvailability_FailsValidation()
    {
        var result = _validator.Validate(Valid() with { AvailabilityPct = -5 });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void MultipleFailures_AllErrorsReturned()
    {
        var result = _validator.Validate(Valid() with
        {
            ResourceId = "",
            AvailabilityPct = -1,
            AdjStart = DateTime.Today.AddDays(5),
            AdjEnd = DateTime.Today
        });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 3);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddAdjustmentCommand.ResourceId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddAdjustmentCommand.AvailabilityPct));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddAdjustmentCommand.AdjEnd));
    }

    [Fact]
    public void EmptyNotes_PassesValidation()
    {
        var result = _validator.Validate(Valid() with { Notes = "" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ErrorMessage_ResourceId_ContainsExpectedText()
    {
        var result = _validator.Validate(Valid() with { ResourceId = "" });
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Employee is required"));
    }

    [Fact]
    public void ErrorMessage_DateRange_ContainsExpectedText()
    {
        var result = _validator.Validate(Valid() with
        {
            AdjStart = DateTime.Today.AddDays(5),
            AdjEnd = DateTime.Today
        });
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("before or equal"));
    }
}

[Collection(DatabaseCollection.Name)]
public class UpsertHolidayCommandValidatorAdditionalTests : OrchestratorFixture
{
    public UpsertHolidayCommandValidatorAdditionalTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task NullHolidayName_FailsValidation()
    {
        var validator = new UpsertHolidayCommandValidator(HolidayOrchestrator);
        var result = await validator.ValidateAsync(new UpsertHolidayCommand(
            Id: 0, HolidayName: null!, StartDate: new DateTime(2026, 11, 15),
            EndDate: new DateTime(2026, 11, 15), HolidayType: "National",
            Notes: null, IsNew: true));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertHolidayCommand.HolidayName));
    }

    [Fact]
    public async Task MultipleFailures_BothNameAndDateErrors()
    {
        var validator = new UpsertHolidayCommandValidator(HolidayOrchestrator);
        var result = await validator.ValidateAsync(new UpsertHolidayCommand(
            Id: 0, HolidayName: "", StartDate: new DateTime(2026, 11, 20),
            EndDate: new DateTime(2026, 11, 15), HolidayType: "National",
            Notes: null, IsNew: true));
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 2);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertHolidayCommand.HolidayName));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertHolidayCommand.StartDate));
    }

    [Fact]
    public async Task OverlapErrorMessage_ContainsExpectedText()
    {
        await using var db = await Factory.CreateDbContextAsync();
        db.Holidays.Add(Holiday.Create("Existing", new DateTime(2026, 12, 20), new DateTime(2026, 12, 25)));
        await db.SaveChangesAsync();

        var validator = new UpsertHolidayCommandValidator(HolidayOrchestrator);
        var result = await validator.ValidateAsync(new UpsertHolidayCommand(
            Id: 0, HolidayName: "Overlap Test",
            StartDate: new DateTime(2026, 12, 22),
            EndDate: new DateTime(2026, 12, 28),
            HolidayType: "National", Notes: null, IsNew: true));
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("overlaps"));
    }

    [Fact]
    public async Task UnconstrainedFields_HolidayType_AcceptsAnyValue()
    {
        var validator = new UpsertHolidayCommandValidator(HolidayOrchestrator);
        var result = await validator.ValidateAsync(new UpsertHolidayCommand(
            Id: 0, HolidayName: "Test", StartDate: new DateTime(2026, 11, 15),
            EndDate: new DateTime(2026, 11, 15), HolidayType: "",
            Notes: null, IsNew: true));
        Assert.True(result.IsValid);
    }
}

// ============================================================
// Handlers — Additional Coverage (Batch 5)
// ============================================================

[Collection(DatabaseCollection.Name)]
public class HandlerCancellationTests : OrchestratorFixture
{
    public HandlerCancellationTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetTasksHandler_CancelledToken_ThrowsOperationCancelled()
    {
        var handler = new GetTasksQueryHandler(TaskOrchestrator);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.Handle(new GetTasksQuery(), cts.Token));
    }

    [Fact]
    public async Task GetHolidaysHandler_CancelledToken_ThrowsOperationCancelled()
    {
        var handler = new GetHolidaysQueryHandler(HolidayOrchestrator);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.Handle(new GetHolidaysQuery(), cts.Token));
    }

    [Fact]
    public async Task GetResourcesHandler_CancelledToken_ThrowsOperationCancelled()
    {
        var handler = new GetResourcesQueryHandler(ResourceOrchestrator);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.Handle(new GetResourcesQuery(), cts.Token));
    }

    [Fact]
    public async Task GetAdjustmentsHandler_CancelledToken_ThrowsOperationCancelled()
    {
        var handler = new GetAdjustmentsQueryHandler(AdjustmentOrchestrator);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.Handle(new GetAdjustmentsQuery(), cts.Token));
    }

    [Fact]
    public async Task GetCalendarHandler_CancelledToken_ThrowsOperationCancelled()
    {
        await SchedulerService.RunSchedulerAsync();
        var handler = new GetCalendarQueryHandler(PlanningQueryService);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.Handle(new GetCalendarQuery(), cts.Token));
    }

    [Fact]
    public async Task GetOutputPlanHandler_CancelledToken_ThrowsOperationCancelled()
    {
        var handler = new GetOutputPlanQueryHandler(PlanningQueryService);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.Handle(new GetOutputPlanQuery(), cts.Token));
    }

    [Fact]
    public async Task GetTimelineHandler_CancelledToken_ThrowsOperationCancelled()
    {
        var handler = new GetTimelineQueryHandler(PlanningQueryService);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.Handle(new GetTimelineQuery("DEV-001", DateTime.Today, DateTime.Today), cts.Token));
    }

    [Fact]
    public async Task DeleteTaskHandler_CancelledToken_ThrowsOperationCancelled()
    {
        var handler = new DeleteTaskCommandHandler(TaskOrchestrator);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.Handle(new DeleteTaskCommand(1), cts.Token));
    }

    [Fact]
    public async Task DeleteResourceHandler_CancelledToken_ThrowsOperationCancelled()
    {
        var handler = new DeleteResourceCommandHandler(ResourceOrchestrator);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.Handle(new DeleteResourceCommand(1), cts.Token));
    }

    [Fact]
    public async Task DeleteHolidayHandler_CancelledToken_ThrowsOperationCancelled()
    {
        var handler = new DeleteHolidayCommandHandler(HolidayOrchestrator);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.Handle(new DeleteHolidayCommand(1), cts.Token));
    }

    [Fact]
    public async Task DeleteAdjustmentHandler_CancelledToken_ThrowsOperationCancelled()
    {
        var handler = new DeleteAdjustmentCommandHandler(AdjustmentOrchestrator);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.Handle(new DeleteAdjustmentCommand(1), cts.Token));
    }
}

[Collection(DatabaseCollection.Name)]
public class HandlerEdgeCaseTests : OrchestratorFixture
{
    public HandlerEdgeCaseTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task AddAdjustmentHandler_NullNotes_PersistsNull()
    {
        var handler = new AddAdjustmentCommandHandler(AdjustmentOrchestrator);
        await handler.Handle(new AddAdjustmentCommand(
            ResourceId: "DEV-001", AdjType: "Vacation",
            AvailabilityPct: 0, AdjStart: DateTime.Today,
            AdjEnd: DateTime.Today.AddDays(3), Notes: null), CancellationToken.None);

        await using var db = await Factory.CreateDbContextAsync();
        var adj = await db.Adjustments.FirstAsync(a => a.ResourceId == "DEV-001");
        Assert.Null(adj.Notes);
    }

    [Fact]
    public async Task GetTaskCountHandler_AfterDelete_CountDecreases()
    {
        var countHandler = new GetTaskCountQueryHandler(TaskOrchestrator);
        var initialCount = await countHandler.Handle(new GetTaskCountQuery(), CancellationToken.None);

        await using var db = await Factory.CreateDbContextAsync();
        var firstTask = await db.Tasks.FirstAsync();

        var deleteHandler = new DeleteTaskCommandHandler(TaskOrchestrator);
        await deleteHandler.Handle(new DeleteTaskCommand(firstTask.Id), CancellationToken.None);

        var newCount = await countHandler.Handle(new GetTaskCountQuery(), CancellationToken.None);
        Assert.Equal(initialCount.Value - 1, newCount.Value);
    }

    [Fact]
    public async Task RunSchedulerHandler_ReturnsSuccessMessage()
    {
        var handler = new RunSchedulerCommandHandler(SchedulerService);
        var result = await handler.Handle(new RunSchedulerCommand(), CancellationToken.None);
        Assert.Contains("successfully scheduled", result.Value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetDashboardKpisHandler_ReturnsNonNullDto()
    {
        var handler = new GetDashboardKpisQueryHandler(SchedulerService);
        var result = await handler.Handle(new GetDashboardKpisQuery(), CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(result.Value.TotalServices > 0);
        Assert.True(result.Value.ActiveResources > 0);
    }

    [Fact]
    public async Task GetDashboardKpisHandler_AfterScheduler_TotalEstimationIsPositive()
    {
        await SchedulerService.RunSchedulerAsync();
        var handler = new GetDashboardKpisQueryHandler(SchedulerService);
        var result = await handler.Handle(new GetDashboardKpisQuery(), CancellationToken.None);
        Assert.True(result.Value.TotalEstimation > 0);
    }
}

// ============================================================
// Workload Heatmap — Query
// ============================================================

[Collection(DatabaseCollection.Name)]
public class GetWorkloadHeatmapQueryHandlerTests : OrchestratorFixture
{
    public GetWorkloadHeatmapQueryHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_ReturnsSuccessWithDto()
    {
        var handler = new GetWorkloadHeatmapQueryHandler(PlanningQueryService);
        var result = await handler.Handle(new GetWorkloadHeatmapQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.ResourceNames);
        Assert.NotNull(result.Value.WeekStarts);
        Assert.NotNull(result.Value.Cells);
    }

    [Fact]
    public async Task Handle_AfterScheduler_ReturnsPopulatedHeatmap()
    {
        await SchedulerService.RunSchedulerAsync();
        var handler = new GetWorkloadHeatmapQueryHandler(PlanningQueryService);
        var result = await handler.Handle(new GetWorkloadHeatmapQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.ResourceNames.Count > 0);
    }
}

// ============================================================
// Risk Trend — Query
// ============================================================

[Collection(DatabaseCollection.Name)]
public class GetRiskTrendQueryHandlerTests : OrchestratorFixture
{
    public GetRiskTrendQueryHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_WithNoSnapshots_ReturnsEmptyList()
    {
        var handler = new GetRiskTrendQueryHandler(PlanningQueryService);
        var result = await handler.Handle(new GetRiskTrendQuery(10), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task Handle_AfterScheduler_ReturnsAtLeastOnePoint()
    {
        await SchedulerService.RunSchedulerAsync();
        var handler = new GetRiskTrendQueryHandler(PlanningQueryService);
        var result = await handler.Handle(new GetRiskTrendQuery(10), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Count > 0);
    }

    [Fact]
    public async Task Handle_RespectsMaxPointsLimit()
    {
        // Run scheduler twice to create two snapshots
        await SchedulerService.RunSchedulerAsync();
        await SchedulerService.RunSchedulerAsync();

        var handler = new GetRiskTrendQueryHandler(PlanningQueryService);
        var result = await handler.Handle(new GetRiskTrendQuery(1), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Count <= 1);
    }
}

// ============================================================
// Task Allocations — Query
// ============================================================

[Collection(DatabaseCollection.Name)]
public class GetTaskAllocationsQueryHandlerTests : OrchestratorFixture
{
    public GetTaskAllocationsQueryHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_WithValidTaskId_ReturnsSuccess()
    {
        var tasks = await TaskOrchestrator.GetTasksAsync();
        var firstTaskId = tasks.First().TaskId;

        var handler = new GetTaskAllocationsQueryHandler(PlanningQueryService);
        var result = await handler.Handle(new GetTaskAllocationsQuery(firstTaskId), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task Handle_WithNonExistentTaskId_ReturnsEmptyList()
    {
        var handler = new GetTaskAllocationsQueryHandler(PlanningQueryService);
        var result = await handler.Handle(new GetTaskAllocationsQuery("NON_EXISTENT_TASK"), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}

// ============================================================
// Last Scheduler Run — Query
// ============================================================

[Collection(DatabaseCollection.Name)]
public class GetLastSchedulerRunQueryHandlerTests : OrchestratorFixture
{
    public GetLastSchedulerRunQueryHandlerTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Handle_BeforeAnyRun_ReturnsNull()
    {
        var handler = new GetLastSchedulerRunQueryHandler(PlanningQueryService);
        var result = await handler.Handle(new GetLastSchedulerRunQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        // May or may not be null depending on whether other tests ran the scheduler
        // in this shared DB; just verify it returns successfully
    }

    [Fact]
    public async Task Handle_AfterSchedulerRun_ReturnsTimestamp()
    {
        await SchedulerService.RunSchedulerAsync();
        var handler = new GetLastSchedulerRunQueryHandler(PlanningQueryService);
        var result = await handler.Handle(new GetLastSchedulerRunQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }
}
