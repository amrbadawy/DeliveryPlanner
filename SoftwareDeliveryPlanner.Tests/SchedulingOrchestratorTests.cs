using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Data;
using SoftwareDeliveryPlanner.Infrastructure.Services;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Tests;

/// <summary>
/// Integration-style tests for SchedulingOrchestrator using an in-memory database.
/// These tests verify that the orchestrator correctly adapts SchedulingEngine results
/// into the DashboardKpisDto expected by the Application layer.
/// </summary>
public class SchedulingOrchestratorTests : IAsyncDisposable
{
    private readonly IDbContextFactory<PlannerDbContext> _factory;

    public SchedulingOrchestratorTests()
    {
        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);

        // Seed default data via the first context
        using var db = new PlannerDbContext(options);
        db.Database.EnsureCreated();
        db.InitializeDefaultData();
    }

    public async ValueTask DisposeAsync() => await Task.CompletedTask;

    #region RunSchedulerAsync

    [Fact]
    public async Task RunSchedulerAsync_WithTasks_ReturnsSuccessMessage()
    {
        var orchestrator = new SchedulingOrchestrator(_factory);
        var result = await orchestrator.RunSchedulerAsync();
        Assert.Contains("successfully scheduled", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunSchedulerAsync_NoTasks_ReturnsNoTasksMessage()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Tasks.RemoveRange(db.Tasks);
        await db.SaveChangesAsync();

        var orchestrator = new SchedulingOrchestrator(_factory);
        var result = await orchestrator.RunSchedulerAsync();
        Assert.Equal("No tasks to schedule", result);
    }

    [Fact]
    public async Task RunSchedulerAsync_RespectsDefaultData()
    {
        // Default data has tasks; scheduler should complete and produce allocations
        var orchestrator = new SchedulingOrchestrator(_factory);
        await orchestrator.RunSchedulerAsync();

        await using var db = await _factory.CreateDbContextAsync();
        Assert.True(db.Allocations.Any());
    }

    #endregion

    #region GetDashboardKpisAsync — shape & types

    [Fact]
    public async Task GetDashboardKpisAsync_ReturnsNonNullDto()
    {
        var orchestrator = new SchedulingOrchestrator(_factory);
        var dto = await orchestrator.GetDashboardKpisAsync();
        Assert.NotNull(dto);
    }

    [Fact]
    public async Task GetDashboardKpisAsync_TotalServices_IsPositive()
    {
        var orchestrator = new SchedulingOrchestrator(_factory);
        var dto = await orchestrator.GetDashboardKpisAsync();
        Assert.True(dto.TotalServices > 0);
    }

    [Fact]
    public async Task GetDashboardKpisAsync_ActiveResources_IsPositive()
    {
        var orchestrator = new SchedulingOrchestrator(_factory);
        var dto = await orchestrator.GetDashboardKpisAsync();
        Assert.True(dto.ActiveResources > 0);
    }

    [Fact]
    public async Task GetDashboardKpisAsync_TotalCapacity_IsPositive()
    {
        var orchestrator = new SchedulingOrchestrator(_factory);
        var dto = await orchestrator.GetDashboardKpisAsync();
        Assert.True(dto.TotalCapacity > 0);
    }

    [Fact]
    public async Task GetDashboardKpisAsync_RiskCounts_AreNonNegative()
    {
        var orchestrator = new SchedulingOrchestrator(_factory);
        var dto = await orchestrator.GetDashboardKpisAsync();
        Assert.True(dto.OnTrack >= 0);
        Assert.True(dto.AtRisk >= 0);
        Assert.True(dto.Late >= 0);
    }

    [Fact]
    public async Task GetDashboardKpisAsync_AvgAssigned_IsNonNegative()
    {
        var orchestrator = new SchedulingOrchestrator(_factory);
        var dto = await orchestrator.GetDashboardKpisAsync();
        Assert.True(dto.AvgAssigned >= 0);
    }

    #endregion

    #region GetDashboardKpisAsync — OverallFinish mapping

    [Fact]
    public async Task GetDashboardKpisAsync_NoTasksWithFinish_OverallFinishIsNull()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Tasks.RemoveRange(db.Tasks);
        // Add a task with no planned finish
        db.Tasks.Add(new TaskItem
        {
            TaskId = "SV-99",
            ServiceName = "No Finish Task",
            DevEstimation = 5,
            Priority = 5,
            PlannedFinish = null
        });
        await db.SaveChangesAsync();

        var orchestrator = new SchedulingOrchestrator(_factory);
        var dto = await orchestrator.GetDashboardKpisAsync();
        Assert.Null(dto.OverallFinish);
    }

    [Fact]
    public async Task GetDashboardKpisAsync_TasksWithFinish_OverallFinishIsMax()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Tasks.RemoveRange(db.Tasks);

        var d1 = new DateTime(2026, 8, 1);
        var d2 = new DateTime(2026, 9, 15);

        db.Tasks.Add(new TaskItem
        {
            TaskId = "SV-A1",
            ServiceName = "Task A",
            DevEstimation = 5,
            Priority = 5,
            PlannedFinish = d1
        });
        db.Tasks.Add(new TaskItem
        {
            TaskId = "SV-B2",
            ServiceName = "Task B",
            DevEstimation = 5,
            Priority = 5,
            PlannedFinish = d2
        });
        await db.SaveChangesAsync();

        var orchestrator = new SchedulingOrchestrator(_factory);
        var dto = await orchestrator.GetDashboardKpisAsync();

        // The orchestrator maps overall_finish: if overall_finish == DateTime.MinValue → null
        // If tasks have PlannedFinish, overallFinish should be d2
        Assert.Equal(d2, dto.OverallFinish);
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task RunSchedulerAsync_CancelledToken_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var orchestrator = new SchedulingOrchestrator(_factory);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            orchestrator.RunSchedulerAsync(cts.Token));
    }

    [Fact]
    public async Task GetDashboardKpisAsync_CancelledToken_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var orchestrator = new SchedulingOrchestrator(_factory);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            orchestrator.GetDashboardKpisAsync(cts.Token));
    }

    #endregion

    #region Holiday CRUD via Orchestrator

    [Fact]
    public async Task GetHolidaysAsync_ReturnsOrderedByStartDate()
    {
        var orchestrator = new SchedulingOrchestrator(_factory);
        var holidays = await orchestrator.GetHolidaysAsync();

        Assert.True(holidays.Count > 1);
        for (int i = 1; i < holidays.Count; i++)
        {
            Assert.True(holidays[i].StartDate >= holidays[i - 1].StartDate,
                $"Holiday at index {i} ({holidays[i].StartDate:d}) is before index {i - 1} ({holidays[i - 1].StartDate:d})");
        }
    }

    [Fact]
    public async Task UpsertHolidayAsync_NewHoliday_PersistsInDb()
    {
        var orchestrator = new SchedulingOrchestrator(_factory);
        var holiday = new Holiday
        {
            HolidayName = "Test New Holiday",
            StartDate = new DateTime(2026, 12, 25),
            EndDate = new DateTime(2026, 12, 25),
            HolidayType = "National"
        };

        await orchestrator.UpsertHolidayAsync(holiday, isNew: true);

        await using var db = await _factory.CreateDbContextAsync();
        var persisted = await db.Holidays.FirstOrDefaultAsync(h => h.HolidayName == "Test New Holiday");
        Assert.NotNull(persisted);
        Assert.Equal(new DateTime(2026, 12, 25), persisted.StartDate);
    }

    [Fact]
    public async Task UpsertHolidayAsync_UpdateExisting_ChangesFields()
    {
        // Arrange: get an existing holiday
        await using var db = await _factory.CreateDbContextAsync();
        var existing = await db.Holidays.FirstAsync();
        var existingId = existing.Id;
        var originalName = existing.HolidayName;

        // Act: update name and type via orchestrator
        var updated = new Holiday
        {
            Id = existingId,
            HolidayName = "Updated Holiday Name",
            StartDate = existing.StartDate,
            EndDate = existing.EndDate,
            HolidayType = "Religious",
            Notes = existing.Notes
        };

        var orchestrator = new SchedulingOrchestrator(_factory);
        await orchestrator.UpsertHolidayAsync(updated, isNew: false);

        // Assert
        await using var verifyDb = await _factory.CreateDbContextAsync();
        var reloaded = await verifyDb.Holidays.FirstOrDefaultAsync(h => h.Id == existingId);
        Assert.NotNull(reloaded);
        Assert.Equal("Updated Holiday Name", reloaded.HolidayName);
        Assert.Equal("Religious", reloaded.HolidayType);
    }

    [Fact]
    public async Task UpsertHolidayAsync_UpdateNonExistent_DoesNotThrow()
    {
        var orchestrator = new SchedulingOrchestrator(_factory);
        var ghost = new Holiday
        {
            Id = 999999,
            HolidayName = "Ghost Holiday",
            StartDate = new DateTime(2026, 12, 31),
            EndDate = new DateTime(2026, 12, 31),
            HolidayType = "National"
        };

        var exception = await Record.ExceptionAsync(() =>
            orchestrator.UpsertHolidayAsync(ghost, isNew: false));

        Assert.Null(exception);
    }

    [Fact]
    public async Task DeleteHolidayAsync_ExistingHoliday_RemovesFromDb()
    {
        // Arrange: get an existing holiday id
        await using var db = await _factory.CreateDbContextAsync();
        var existing = await db.Holidays.FirstAsync();
        var existingId = existing.Id;

        // Act
        var orchestrator = new SchedulingOrchestrator(_factory);
        await orchestrator.DeleteHolidayAsync(existingId);

        // Assert
        await using var verifyDb = await _factory.CreateDbContextAsync();
        var deleted = await verifyDb.Holidays.FirstOrDefaultAsync(h => h.Id == existingId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteHolidayAsync_NonExistent_DoesNotThrow()
    {
        var orchestrator = new SchedulingOrchestrator(_factory);

        var exception = await Record.ExceptionAsync(() =>
            orchestrator.DeleteHolidayAsync(999999));

        Assert.Null(exception);
    }

    #endregion

    #region HasHolidayOverlapAsync

    [Fact]
    public async Task HasHolidayOverlapAsync_OverlappingRange_ReturnsTrue()
    {
        // Eid Al-Fitr: Mar 30 - Apr 2, 2026. Overlap with Mar 31 - Apr 1.
        var orchestrator = new SchedulingOrchestrator(_factory);
        var result = await orchestrator.HasHolidayOverlapAsync(
            new DateTime(2026, 3, 31), new DateTime(2026, 4, 1));

        Assert.True(result);
    }

    [Fact]
    public async Task HasHolidayOverlapAsync_NonOverlappingRange_ReturnsFalse()
    {
        // No holiday in July 2026
        var orchestrator = new SchedulingOrchestrator(_factory);
        var result = await orchestrator.HasHolidayOverlapAsync(
            new DateTime(2026, 7, 10), new DateTime(2026, 7, 15));

        Assert.False(result);
    }

    [Fact]
    public async Task HasHolidayOverlapAsync_ExactSameRange_ReturnsTrue()
    {
        // National Day: Sep 23, 2026
        var orchestrator = new SchedulingOrchestrator(_factory);
        var result = await orchestrator.HasHolidayOverlapAsync(
            new DateTime(2026, 9, 23), new DateTime(2026, 9, 23));

        Assert.True(result);
    }

    [Fact]
    public async Task HasHolidayOverlapAsync_WithExcludeId_ExcludesSelf()
    {
        // Find the National Day holiday and exclude it by its Id
        await using var db = await _factory.CreateDbContextAsync();
        var nationalDay = await db.Holidays.FirstAsync(h => h.StartDate == new DateTime(2026, 9, 23));
        var nationalDayId = nationalDay.Id;

        var orchestrator = new SchedulingOrchestrator(_factory);
        var result = await orchestrator.HasHolidayOverlapAsync(
            new DateTime(2026, 9, 23), new DateTime(2026, 9, 23), excludeId: nationalDayId);

        Assert.False(result);
    }

    [Fact]
    public async Task HasHolidayOverlapAsync_AdjacentDates_ReturnsFalse()
    {
        // Eid Al-Fitr ends Apr 2, 2026. Check Apr 3 (day after) — no overlap.
        var orchestrator = new SchedulingOrchestrator(_factory);
        var result = await orchestrator.HasHolidayOverlapAsync(
            new DateTime(2026, 4, 3), new DateTime(2026, 4, 3));

        Assert.False(result);
    }

    #endregion

    #region CopyHolidaysToYearAsync

    [Fact]
    public async Task CopyHolidaysToYearAsync_CopiesHolidaysToNewYear()
    {
        var orchestrator = new SchedulingOrchestrator(_factory);
        var copied = await orchestrator.CopyHolidaysToYearAsync(2026, 2027);

        Assert.True(copied > 0);

        await using var db = await _factory.CreateDbContextAsync();
        var holidays2027 = await db.Holidays.Where(h => h.StartDate.Year == 2027).ToListAsync();
        Assert.True(holidays2027.Count > 0);
    }

    [Fact]
    public async Task CopyHolidaysToYearAsync_SkipsOverlappingTargets()
    {
        // Pre-seed a holiday in 2028 that would overlap with one copied from 2026
        // National Day 2026 = Sep 23 → 2028 = Sep 23
        await using var db = await _factory.CreateDbContextAsync();
        db.Holidays.Add(new Holiday
        {
            HolidayName = "Pre-existing 2028 Holiday",
            StartDate = new DateTime(2028, 9, 23),
            EndDate = new DateTime(2028, 9, 23),
            HolidayType = "National"
        });
        await db.SaveChangesAsync();

        var orchestrator = new SchedulingOrchestrator(_factory);

        // Get count of 2026 holidays
        await using var countDb = await _factory.CreateDbContextAsync();
        var source2026Count = await countDb.Holidays.CountAsync(h => h.StartDate.Year == 2026);

        var copied = await orchestrator.CopyHolidaysToYearAsync(2026, 2028);

        // At least one should be skipped
        Assert.True(copied < source2026Count);
    }

    [Fact]
    public async Task CopyHolidaysToYearAsync_NothingToCopy_ReturnsZero()
    {
        // Copy from year 2099 which has no holidays
        var orchestrator = new SchedulingOrchestrator(_factory);
        var copied = await orchestrator.CopyHolidaysToYearAsync(2099, 2100);

        Assert.Equal(0, copied);
    }

    #endregion

    #region GetHolidayWorkingDaysLostAsync

    [Fact]
    public async Task GetHolidayWorkingDaysLostAsync_FullWeek_ReturnsFive()
    {
        // Sun Jun 7 to Sat Jun 13, 2026 — working days: Sun, Mon, Tue, Wed, Thu = 5
        var orchestrator = new SchedulingOrchestrator(_factory);
        var result = await orchestrator.GetHolidayWorkingDaysLostAsync(
            new DateTime(2026, 6, 7), new DateTime(2026, 6, 13));

        Assert.Equal(5, result);
    }

    [Fact]
    public async Task GetHolidayWorkingDaysLostAsync_WeekendOnly_ReturnsZero()
    {
        // Fri Jun 5 to Sat Jun 6, 2026 — both are weekend
        var orchestrator = new SchedulingOrchestrator(_factory);
        var result = await orchestrator.GetHolidayWorkingDaysLostAsync(
            new DateTime(2026, 6, 5), new DateTime(2026, 6, 6));

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetHolidayWorkingDaysLostAsync_SingleWorkday_ReturnsOne()
    {
        // Sun Jun 7, 2026 — Sunday is a workday
        var orchestrator = new SchedulingOrchestrator(_factory);
        var result = await orchestrator.GetHolidayWorkingDaysLostAsync(
            new DateTime(2026, 6, 7), new DateTime(2026, 6, 7));

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task GetHolidayWorkingDaysLostAsync_SingleWeekendDay_ReturnsZero()
    {
        // Fri Jun 5, 2026 — Friday is weekend
        var orchestrator = new SchedulingOrchestrator(_factory);
        var result = await orchestrator.GetHolidayWorkingDaysLostAsync(
            new DateTime(2026, 6, 5), new DateTime(2026, 6, 5));

        Assert.Equal(0, result);
    }

    #endregion

    #region Resource/Task cancellation

    [Fact]
    public async Task GetTasksAsync_CancelledToken_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var orchestrator = new SchedulingOrchestrator(_factory);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            orchestrator.GetTasksAsync(cts.Token));
    }

    [Fact]
    public async Task GetHolidaysAsync_CancelledToken_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var orchestrator = new SchedulingOrchestrator(_factory);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            orchestrator.GetHolidaysAsync(cts.Token));
    }

    #endregion
}

// ---------------------------------------------------------------------------
// Test helper: minimal IDbContextFactory implementation
// ---------------------------------------------------------------------------

file sealed class TestDbContextFactory : IDbContextFactory<PlannerDbContext>
{
    private readonly DbContextOptions<PlannerDbContext> _options;

    public TestDbContextFactory(DbContextOptions<PlannerDbContext> options)
    {
        _options = options;
    }

    public PlannerDbContext CreateDbContext() => new(_options);

    public Task<PlannerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new PlannerDbContext(_options));
    }
}
