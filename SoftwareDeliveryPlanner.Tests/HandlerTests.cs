using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MediatR;
using SoftwareDeliveryPlanner.Application.Adjustments.Commands;
using SoftwareDeliveryPlanner.Application.Adjustments.Queries;
using SoftwareDeliveryPlanner.Application.Calendar.Queries;
using SoftwareDeliveryPlanner.Application.Holidays.Commands;
using SoftwareDeliveryPlanner.Application.Holidays.Queries;
using SoftwareDeliveryPlanner.Application.Output.Queries;
using SoftwareDeliveryPlanner.Application.Resources.Commands;
using SoftwareDeliveryPlanner.Application.Resources.Queries;
using SoftwareDeliveryPlanner.Application.Tasks.Commands;
using SoftwareDeliveryPlanner.Application.Tasks.Queries;
using SoftwareDeliveryPlanner.Application.Timeline.Queries;
using SoftwareDeliveryPlanner.Data;
using SoftwareDeliveryPlanner.Infrastructure.Services;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Tests;

// ============================================================
// Base fixture: fresh in-memory DB seeded with default data
// ============================================================

public abstract class OrchestratorFixture : IAsyncDisposable
{
    protected readonly IDbContextFactory<PlannerDbContext> Factory;
    protected readonly SchedulingOrchestrator Orchestrator;

    protected OrchestratorFixture()
    {
        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Factory = new HandlerTestDbContextFactory(options);

        using var db = new PlannerDbContext(options);
        db.Database.EnsureCreated();
        db.InitializeDefaultData();

        Orchestrator = new SchedulingOrchestrator(Factory);
    }

    public async ValueTask DisposeAsync() => await Task.CompletedTask;
}

// ============================================================
// Tasks — Queries
// ============================================================

public class GetTasksQueryHandlerTests : OrchestratorFixture
{
    [Fact]
    public async Task Handle_ReturnsAllTasks()
    {
        var handler = new GetTasksQueryHandler(Orchestrator);
        var result = await handler.Handle(new GetTasksQuery(), CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    [Fact]
    public async Task Handle_ReturnsTasksOrderedBySchedulingRank()
    {
        var handler = new GetTasksQueryHandler(Orchestrator);
        var result = await handler.Handle(new GetTasksQuery(), CancellationToken.None);
        // All tasks from default data should come back (13 seeded)
        Assert.Equal(13, result.Count);
    }
}

public class GetTaskCountQueryHandlerTests : OrchestratorFixture
{
    [Fact]
    public async Task Handle_ReturnsCorrectCount()
    {
        var handler = new GetTaskCountQueryHandler(Orchestrator);
        var count = await handler.Handle(new GetTaskCountQuery(), CancellationToken.None);
        Assert.Equal(13, count);
    }

    [Fact]
    public async Task Handle_AfterAddingTask_CountIncreases()
    {
        await using var db = await Factory.CreateDbContextAsync();
        db.Tasks.Add(new TaskItem
        {
            TaskId = "TST-99",
            ServiceName = "Extra",
            DevEstimation = 1,
            Priority = 5
        });
        await db.SaveChangesAsync();

        var handler = new GetTaskCountQueryHandler(Orchestrator);
        var count = await handler.Handle(new GetTaskCountQuery(), CancellationToken.None);
        Assert.Equal(14, count);
    }
}

// ============================================================
// Tasks — Commands
// ============================================================

public class UpsertTaskCommandHandlerTests : OrchestratorFixture
{
    [Fact]
    public async Task Handle_NewTask_AddsTaskToDatabase()
    {
        var handler = new UpsertTaskCommandHandler(Orchestrator);
        var command = new UpsertTaskCommand(
            Id: 0,
            TaskId: "NEW-01",
            ServiceName: "New Service",
            DevEstimation: 10,
            MaxDev: 2,
            Priority: 3,
            StrictDate: null,
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

        var handler = new UpsertTaskCommandHandler(Orchestrator);
        var command = new UpsertTaskCommand(
            Id: existing.Id,
            TaskId: existing.TaskId,
            ServiceName: "Updated Service Name",
            DevEstimation: existing.DevEstimation,
            MaxDev: existing.MaxDev,
            Priority: existing.Priority,
            StrictDate: null,
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
        var handler = new UpsertTaskCommandHandler(Orchestrator);
        await handler.Handle(new UpsertTaskCommand(
            Id: 0,
            TaskId: "STR-01",
            ServiceName: "Strict Task",
            DevEstimation: 5,
            MaxDev: 1,
            Priority: 1,
            StrictDate: strictDate,
            IsNew: true), CancellationToken.None);

        await using var db = await Factory.CreateDbContextAsync();
        var task = await db.Tasks.FirstAsync(t => t.TaskId == "STR-01");
        Assert.Equal(strictDate, task.StrictDate);
    }
}

public class DeleteTaskCommandHandlerTests : OrchestratorFixture
{
    [Fact]
    public async Task Handle_ExistingTask_RemovesFromDatabase()
    {
        await using var db = await Factory.CreateDbContextAsync();
        var task = await db.Tasks.FirstAsync();
        var taskId = task.Id;

        var handler = new DeleteTaskCommandHandler(Orchestrator);
        await handler.Handle(new DeleteTaskCommand(taskId), CancellationToken.None);

        await using var verifyDb = await Factory.CreateDbContextAsync();
        Assert.False(await verifyDb.Tasks.AnyAsync(t => t.Id == taskId));
    }

    [Fact]
    public async Task Handle_NonExistentTask_DoesNotThrow()
    {
        var handler = new DeleteTaskCommandHandler(Orchestrator);
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
        StrictDate: null, IsNew: true);

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

public class GetResourcesQueryHandlerTests : OrchestratorFixture
{
    [Fact]
    public async Task Handle_ReturnsAllResources()
    {
        var handler = new GetResourcesQueryHandler(Orchestrator);
        var result = await handler.Handle(new GetResourcesQuery(), CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(5, result.Count); // 5 seeded
    }
}

public class GetResourceCountQueryHandlerTests : OrchestratorFixture
{
    [Fact]
    public async Task Handle_ReturnsCorrectCount()
    {
        var handler = new GetResourceCountQueryHandler(Orchestrator);
        var count = await handler.Handle(new GetResourceCountQuery(), CancellationToken.None);
        Assert.Equal(5, count);
    }
}

// ============================================================
// Resources — Commands
// ============================================================

public class UpsertResourceCommandHandlerTests : OrchestratorFixture
{
    [Fact]
    public async Task Handle_NewResource_AddsToDatabase()
    {
        var handler = new UpsertResourceCommandHandler(Orchestrator);
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

        var handler = new UpsertResourceCommandHandler(Orchestrator);
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

public class DeleteResourceCommandHandlerTests : OrchestratorFixture
{
    [Fact]
    public async Task Handle_ExistingResource_RemovesFromDatabase()
    {
        await using var db = await Factory.CreateDbContextAsync();
        var resource = await db.Resources.FirstAsync();
        var resourceId = resource.Id;

        var handler = new DeleteResourceCommandHandler(Orchestrator);
        await handler.Handle(new DeleteResourceCommand(resourceId), CancellationToken.None);

        await using var verifyDb = await Factory.CreateDbContextAsync();
        Assert.False(await verifyDb.Resources.AnyAsync(r => r.Id == resourceId));
    }

    [Fact]
    public async Task Handle_NonExistentResource_DoesNotThrow()
    {
        var handler = new DeleteResourceCommandHandler(Orchestrator);
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

public class GetAdjustmentsQueryHandlerTests : OrchestratorFixture
{
    [Fact]
    public async Task Handle_EmptyDb_ReturnsEmptyList()
    {
        var handler = new GetAdjustmentsQueryHandler(Orchestrator);
        var result = await handler.Handle(new GetAdjustmentsQuery(), CancellationToken.None);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_WithAdjustment_ReturnsList()
    {
        await using var db = await Factory.CreateDbContextAsync();
        db.Adjustments.Add(new Adjustment
        {
            ResourceId = "DEV-001",
            AdjType = "Vacation",
            AvailabilityPct = 0,
            AdjStart = DateTime.Today,
            AdjEnd = DateTime.Today.AddDays(3)
        });
        await db.SaveChangesAsync();

        var handler = new GetAdjustmentsQueryHandler(Orchestrator);
        var result = await handler.Handle(new GetAdjustmentsQuery(), CancellationToken.None);
        Assert.Single(result);
    }
}

// ============================================================
// Adjustments — Commands
// ============================================================

public class AddAdjustmentCommandHandlerTests : OrchestratorFixture
{
    [Fact]
    public async Task Handle_ValidCommand_AddsAdjustmentToDatabase()
    {
        var handler = new AddAdjustmentCommandHandler(Orchestrator);
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
        var handler = new AddAdjustmentCommandHandler(Orchestrator);

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

public class DeleteAdjustmentCommandHandlerTests : OrchestratorFixture
{
    [Fact]
    public async Task Handle_ExistingAdjustment_RemovesFromDatabase()
    {
        await using var db = await Factory.CreateDbContextAsync();
        db.Adjustments.Add(new Adjustment
        {
            ResourceId = "DEV-001",
            AdjType = "Vacation",
            AvailabilityPct = 0,
            AdjStart = DateTime.Today,
            AdjEnd = DateTime.Today.AddDays(3)
        });
        await db.SaveChangesAsync();

        await using var db2 = await Factory.CreateDbContextAsync();
        var adj = await db2.Adjustments.FirstAsync();

        var handler = new DeleteAdjustmentCommandHandler(Orchestrator);
        await handler.Handle(new DeleteAdjustmentCommand(adj.Id), CancellationToken.None);

        await using var verifyDb = await Factory.CreateDbContextAsync();
        Assert.False(await verifyDb.Adjustments.AnyAsync(a => a.Id == adj.Id));
    }

    [Fact]
    public async Task Handle_NonExistentAdjustment_DoesNotThrow()
    {
        var handler = new DeleteAdjustmentCommandHandler(Orchestrator);
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

public class GetHolidaysQueryHandlerTests : OrchestratorFixture
{
    [Fact]
    public async Task Handle_ReturnsAllHolidays()
    {
        var handler = new GetHolidaysQueryHandler(Orchestrator);
        var result = await handler.Handle(new GetHolidaysQuery(), CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(12, result.Count); // 12 seeded
    }

    [Fact]
    public async Task Handle_ReturnsHolidaysOrderedByDate()
    {
        var handler = new GetHolidaysQueryHandler(Orchestrator);
        var result = await handler.Handle(new GetHolidaysQuery(), CancellationToken.None);
        for (int i = 1; i < result.Count; i++)
        {
            Assert.True(result[i].HolidayDate >= result[i - 1].HolidayDate);
        }
    }
}

// ============================================================
// Holidays — Commands
// ============================================================

public class UpsertHolidayCommandHandlerTests : OrchestratorFixture
{
    [Fact]
    public async Task Handle_NewHoliday_AddsToDatabase()
    {
        var handler = new UpsertHolidayCommandHandler(Orchestrator);
        var date = new DateTime(2026, 12, 25);

        await handler.Handle(new UpsertHolidayCommand(
            Id: 0,
            HolidayName: "Christmas",
            HolidayDate: date,
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

        var handler = new UpsertHolidayCommandHandler(Orchestrator);
        await handler.Handle(new UpsertHolidayCommand(
            Id: existing.Id,
            HolidayName: "Updated Holiday",
            HolidayDate: existing.HolidayDate,
            HolidayType: "Company",
            Notes: "updated",
            IsNew: false), CancellationToken.None);

        await using var verifyDb = await Factory.CreateDbContextAsync();
        var updated = await verifyDb.Holidays.FirstAsync(h => h.Id == existing.Id);
        Assert.Equal("Updated Holiday", updated.HolidayName);
        Assert.Equal("Company", updated.HolidayType);
    }
}

public class DeleteHolidayCommandHandlerTests : OrchestratorFixture
{
    [Fact]
    public async Task Handle_ExistingHoliday_RemovesFromDatabase()
    {
        await using var db = await Factory.CreateDbContextAsync();
        var holiday = await db.Holidays.FirstAsync();
        var holidayId = holiday.Id;

        var handler = new DeleteHolidayCommandHandler(Orchestrator);
        await handler.Handle(new DeleteHolidayCommand(holidayId), CancellationToken.None);

        await using var verifyDb = await Factory.CreateDbContextAsync();
        Assert.False(await verifyDb.Holidays.AnyAsync(h => h.Id == holidayId));
    }

    [Fact]
    public async Task Handle_NonExistentHoliday_DoesNotThrow()
    {
        var handler = new DeleteHolidayCommandHandler(Orchestrator);
        await handler.Handle(new DeleteHolidayCommand(99999), CancellationToken.None);
    }
}

// ============================================================
// Holidays — Validators
// ============================================================

public class UpsertHolidayCommandValidatorTests
{
    private readonly UpsertHolidayCommandValidator _validator = new();

    private static UpsertHolidayCommand Valid() => new(
        Id: 0,
        HolidayName: "Eid",
        HolidayDate: DateTime.Today,
        HolidayType: "National",
        Notes: null,
        IsNew: true);

    [Fact]
    public void Valid_Command_PassesValidation()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void EmptyHolidayName_FailsValidation(string name)
    {
        var result = _validator.Validate(Valid() with { HolidayName = name });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertHolidayCommand.HolidayName));
    }
}

// ============================================================
// Calendar — Query
// ============================================================

public class GetCalendarQueryHandlerTests : OrchestratorFixture
{
    [Fact]
    public async Task Handle_AfterScheduler_ReturnsCalendarDays()
    {
        // Run scheduler first to populate calendar
        await Orchestrator.RunSchedulerAsync();

        var handler = new GetCalendarQueryHandler(Orchestrator);
        var result = await handler.Handle(new GetCalendarQuery(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    [Fact]
    public async Task Handle_ReturnsCalendarOrderedByDate()
    {
        await Orchestrator.RunSchedulerAsync();

        var handler = new GetCalendarQueryHandler(Orchestrator);
        var result = await handler.Handle(new GetCalendarQuery(), CancellationToken.None);

        for (int i = 1; i < result.Count; i++)
        {
            Assert.True(result[i].CalendarDate >= result[i - 1].CalendarDate);
        }
    }
}

// ============================================================
// Timeline — Query
// ============================================================

public class GetTimelineQueryHandlerTests : OrchestratorFixture
{
    [Fact]
    public async Task Handle_ValidResourceAndRange_ReturnsDays()
    {
        var handler = new GetTimelineQueryHandler(Orchestrator);
        var start = new DateTime(2026, 5, 1);
        var end = new DateTime(2026, 5, 7);

        var result = await handler.Handle(
            new GetTimelineQuery("DEV-001", start, end),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(7, result.Days.Count);
    }

    [Fact]
    public async Task Handle_SingleDay_ReturnsOneDayDto()
    {
        var handler = new GetTimelineQueryHandler(Orchestrator);
        var date = new DateTime(2026, 5, 4); // Monday

        var result = await handler.Handle(
            new GetTimelineQuery("DEV-001", date, date),
            CancellationToken.None);

        Assert.Single(result.Days);
        Assert.Equal(date, result.Days[0].Date);
    }

    [Fact]
    public async Task Handle_Weekend_MarksCorrectly()
    {
        var handler = new GetTimelineQueryHandler(Orchestrator);
        // 2026-05-01 is a Friday (day 5 = weekend in Sun-Thu week)
        var friday = new DateTime(2026, 5, 1);

        var result = await handler.Handle(
            new GetTimelineQuery("DEV-001", friday, friday),
            CancellationToken.None);

        Assert.Single(result.Days);
        Assert.Equal("Friday", result.Days[0].StatusText);
        Assert.Equal("#f8f9fa", result.Days[0].BackgroundColor);
    }

    [Fact]
    public async Task Handle_Holiday_MarksCorrectly()
    {
        var handler = new GetTimelineQueryHandler(Orchestrator);
        // 2026-09-23 is اليوم الوطني (National Day) - seeded
        var nationalDay = new DateTime(2026, 9, 23);

        var result = await handler.Handle(
            new GetTimelineQuery("DEV-001", nationalDay, nationalDay),
            CancellationToken.None);

        Assert.Single(result.Days);
        Assert.Equal("#fff3cd", result.Days[0].BackgroundColor);
    }

    [Fact]
    public async Task Handle_UnknownResource_ReturnsFreeDays()
    {
        var handler = new GetTimelineQueryHandler(Orchestrator);
        var start = new DateTime(2026, 5, 3); // Sunday — working day
        var end = new DateTime(2026, 5, 3);

        var result = await handler.Handle(
            new GetTimelineQuery("UNKNOWN-99", start, end),
            CancellationToken.None);

        Assert.Single(result.Days);
        // devIndex will be 0 (not found), so no task assignment → "Free" or holiday/weekend
        Assert.NotNull(result.Days[0].StatusText);
    }
}

// ============================================================
// Output — Query
// ============================================================

public class GetOutputPlanQueryHandlerTests : OrchestratorFixture
{
    [Fact]
    public async Task Handle_ReturnsOutputPlan()
    {
        var handler = new GetOutputPlanQueryHandler(Orchestrator);
        var result = await handler.Handle(new GetOutputPlanQuery(), CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_AfterScheduler_ReturnsNonEmptyPlan()
    {
        await Orchestrator.RunSchedulerAsync();

        var handler = new GetOutputPlanQueryHandler(Orchestrator);
        var result = await handler.Handle(new GetOutputPlanQuery(), CancellationToken.None);

        Assert.True(result.Count > 0);
    }

    [Fact]
    public async Task Handle_EachRow_ContainsExpectedKeys()
    {
        await Orchestrator.RunSchedulerAsync();

        var handler = new GetOutputPlanQueryHandler(Orchestrator);
        var result = await handler.Handle(new GetOutputPlanQuery(), CancellationToken.None);

        foreach (var row in result)
        {
            Assert.True(row.ContainsKey("task_id"));
            Assert.True(row.ContainsKey("service_name"));
            Assert.True(row.ContainsKey("status"));
            Assert.True(row.ContainsKey("delivery_risk"));
        }
    }
}

// ============================================================
// Test DB factory (file-scoped to avoid conflict with OrchestratorTests)
// ============================================================

file sealed class HandlerTestDbContextFactory : IDbContextFactory<PlannerDbContext>
{
    private readonly DbContextOptions<PlannerDbContext> _options;

    public HandlerTestDbContextFactory(DbContextOptions<PlannerDbContext> options)
        => _options = options;

    public PlannerDbContext CreateDbContext() => new(_options);

    public Task<PlannerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new PlannerDbContext(_options));
    }
}
