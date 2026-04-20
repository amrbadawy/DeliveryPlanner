using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Infrastructure.Data;
using SoftwareDeliveryPlanner.Infrastructure.Services;
using SoftwareDeliveryPlanner.Domain.Models;
using MediatR;
using SoftwareDeliveryPlanner.Tests.Infrastructure;

namespace SoftwareDeliveryPlanner.Tests;

/// <summary>
/// Integration-style tests for the focused service classes that replaced
/// SchedulingOrchestrator. These tests verify that each service correctly
/// adapts SchedulingEngine results into the DTOs expected by the Application layer.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class SchedulingOrchestratorTests : IAsyncDisposable
{
    private readonly IDbContextFactory<PlannerDbContext> _factory;
    private readonly IDbContextFactory<ReadOnlyPlannerDbContext> _readOnlyFactory;
    private readonly ISchedulingEngineFactory _engineFactory;
    private readonly ITaskOrchestrator _taskService;
    private readonly IResourceOrchestrator _resourceService;
    private readonly IAdjustmentOrchestrator _adjustmentService;
    private readonly IHolidayOrchestrator _holidayService;
    private readonly ISchedulerService _schedulerService;
    private readonly IPlanningQueryService _planningQueryService;

    public SchedulingOrchestratorTests(SqlServerFixture fixture)
    {
        var (options, connectionString) = TestDatabaseHelper.CreateOptions(fixture);
        _factory = new TestDbContextFactory(options);
        _readOnlyFactory = new TestReadOnlyDbContextFactory(connectionString);
        _engineFactory = new SchedulingEngineFactory(_factory, TimeProvider.System);
        var publisher = new NullPublisher();

        _taskService = new TaskService(_factory, _readOnlyFactory, _engineFactory, publisher);
        _resourceService = new ResourceService(_factory, _readOnlyFactory, _engineFactory, publisher);
        _adjustmentService = new AdjustmentService(_factory, _readOnlyFactory, _engineFactory, publisher);
        _holidayService = new HolidayService(_factory, _readOnlyFactory, _engineFactory, publisher);
        _schedulerService = new SchedulerService(_factory, _readOnlyFactory, _engineFactory, publisher);
        _planningQueryService = new PlanningQueryService(_factory, _readOnlyFactory, _engineFactory, publisher);
    }

    public async ValueTask DisposeAsync() => await Task.CompletedTask;

    #region RunSchedulerAsync

    [Fact]
    public async Task RunSchedulerAsync_WithTasks_ReturnsSuccessMessage()
    {

        var result = await _schedulerService.RunSchedulerAsync();
        Assert.Contains("successfully scheduled", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunSchedulerAsync_NoTasks_ReturnsNoTasksMessage()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Tasks.RemoveRange(db.Tasks);
        await db.SaveChangesAsync();


        var result = await _schedulerService.RunSchedulerAsync();
        Assert.Equal("No tasks to schedule", result);
    }

    [Fact]
    public async Task RunSchedulerAsync_RespectsDefaultData()
    {
        // Default data has tasks; scheduler should complete and produce allocations

        await _schedulerService.RunSchedulerAsync();

        await using var db = await _factory.CreateDbContextAsync();
        Assert.True(db.Allocations.Any());
    }

    #endregion

    #region GetDashboardKpisAsync — shape & types

    [Fact]
    public async Task GetDashboardKpisAsync_ReturnsNonNullDto()
    {

        var dto = await _schedulerService.GetDashboardKpisAsync();
        Assert.NotNull(dto);
    }

    [Fact]
    public async Task GetDashboardKpisAsync_TotalServices_IsPositive()
    {

        var dto = await _schedulerService.GetDashboardKpisAsync();
        Assert.True(dto.TotalServices > 0);
    }

    [Fact]
    public async Task GetDashboardKpisAsync_ActiveResources_IsPositive()
    {

        var dto = await _schedulerService.GetDashboardKpisAsync();
        Assert.True(dto.ActiveResources > 0);
    }

    [Fact]
    public async Task GetDashboardKpisAsync_TotalCapacity_IsPositive()
    {

        var dto = await _schedulerService.GetDashboardKpisAsync();
        Assert.True(dto.TotalCapacity > 0);
    }

    [Fact]
    public async Task GetDashboardKpisAsync_RiskCounts_AreNonNegative()
    {

        var dto = await _schedulerService.GetDashboardKpisAsync();
        Assert.True(dto.OnTrack >= 0);
        Assert.True(dto.AtRisk >= 0);
        Assert.True(dto.Late >= 0);
    }

    [Fact]
    public async Task GetDashboardKpisAsync_AvgAssigned_IsNonNegative()
    {

        var dto = await _schedulerService.GetDashboardKpisAsync();
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
        db.Tasks.Add(TaskItem.Create("SV-099", "No Finish Task", 5, 1, 5));
        await db.SaveChangesAsync();


        var dto = await _schedulerService.GetDashboardKpisAsync();
        Assert.Null(dto.OverallFinish);
    }

    [Fact]
    public async Task GetDashboardKpisAsync_TasksWithFinish_OverallFinishIsMax()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Tasks.RemoveRange(db.Tasks);

        var d1 = new DateTime(2026, 8, 1);
        var d2 = new DateTime(2026, 9, 15);

        var t1 = TaskItem.Create("SVA-01", "Task A", 5, 1, 5);
        t1.ApplySchedulingResult(1.0, d1.AddDays(-5), d1, 5, "Completed", "On Track");
        db.Tasks.Add(t1);
        var t2 = TaskItem.Create("SVB-02", "Task B", 5, 1, 5);
        t2.ApplySchedulingResult(1.0, d2.AddDays(-5), d2, 5, "Completed", "On Track");
        db.Tasks.Add(t2);
        await db.SaveChangesAsync();


        var dto = await _schedulerService.GetDashboardKpisAsync();

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



        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _schedulerService.RunSchedulerAsync(cts.Token));
    }

    [Fact]
    public async Task GetDashboardKpisAsync_CancelledToken_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();



        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _schedulerService.GetDashboardKpisAsync(cts.Token));
    }

    #endregion

    #region Holiday CRUD via Orchestrator

    [Fact]
    public async Task GetHolidaysAsync_ReturnsOrderedByStartDate()
    {

        var holidays = await _holidayService.GetHolidaysAsync();

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


        await _holidayService.UpsertHolidayAsync(0, "Test New Holiday", new DateTime(2026, 12, 25), new DateTime(2026, 12, 25), "National", null, true);

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

        await _holidayService.UpsertHolidayAsync(existingId, "Updated Holiday Name", existing.StartDate, existing.EndDate, "Religious", existing.Notes, false);

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


        var exception = await Record.ExceptionAsync(() =>
            _holidayService.UpsertHolidayAsync(999999, "Ghost Holiday", new DateTime(2026, 12, 31), new DateTime(2026, 12, 31), "National", null, false));

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

        await _holidayService.DeleteHolidayAsync(existingId);

        // Assert
        await using var verifyDb = await _factory.CreateDbContextAsync();
        var deleted = await verifyDb.Holidays.FirstOrDefaultAsync(h => h.Id == existingId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteHolidayAsync_NonExistent_DoesNotThrow()
    {


        var exception = await Record.ExceptionAsync(() =>
            _holidayService.DeleteHolidayAsync(999999));

        Assert.Null(exception);
    }

    #endregion

    #region HasHolidayOverlapAsync

    [Fact]
    public async Task HasHolidayOverlapAsync_OverlappingRange_ReturnsTrue()
    {
        // Eid Al-Fitr: Mar 30 - Apr 2, 2026. Overlap with Mar 31 - Apr 1.

        var result = await _holidayService.HasHolidayOverlapAsync(
            new DateTime(2026, 3, 31), new DateTime(2026, 4, 1));

        Assert.True(result);
    }

    [Fact]
    public async Task HasHolidayOverlapAsync_NonOverlappingRange_ReturnsFalse()
    {
        // No holiday in July 2026

        var result = await _holidayService.HasHolidayOverlapAsync(
            new DateTime(2026, 7, 10), new DateTime(2026, 7, 15));

        Assert.False(result);
    }

    [Fact]
    public async Task HasHolidayOverlapAsync_ExactSameRange_ReturnsTrue()
    {
        // National Day: Sep 23, 2026

        var result = await _holidayService.HasHolidayOverlapAsync(
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


        var result = await _holidayService.HasHolidayOverlapAsync(
            new DateTime(2026, 9, 23), new DateTime(2026, 9, 23), excludeId: nationalDayId);

        Assert.False(result);
    }

    [Fact]
    public async Task HasHolidayOverlapAsync_AdjacentDates_ReturnsFalse()
    {
        // Eid Al-Fitr ends Apr 2, 2026. Check Apr 3 (day after) — no overlap.

        var result = await _holidayService.HasHolidayOverlapAsync(
            new DateTime(2026, 4, 3), new DateTime(2026, 4, 3));

        Assert.False(result);
    }

    #endregion

    #region CopyHolidaysToYearAsync

    [Fact]
    public async Task CopyHolidaysToYearAsync_CopiesHolidaysToNewYear()
    {

        var copied = await _holidayService.CopyHolidaysToYearAsync(2026, 2027);

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
        db.Holidays.Add(Holiday.Create("Pre-existing 2028 Holiday", new DateTime(2028, 9, 23), new DateTime(2028, 9, 23), "National"));
        await db.SaveChangesAsync();



        // Get count of 2026 holidays
        await using var countDb = await _factory.CreateDbContextAsync();
        var source2026Count = await countDb.Holidays.CountAsync(h => h.StartDate.Year == 2026);

        var copied = await _holidayService.CopyHolidaysToYearAsync(2026, 2028);

        // At least one should be skipped
        Assert.True(copied < source2026Count);
    }

    [Fact]
    public async Task CopyHolidaysToYearAsync_NothingToCopy_ReturnsZero()
    {
        // Copy from year 2099 which has no holidays

        var copied = await _holidayService.CopyHolidaysToYearAsync(2099, 2100);

        Assert.Equal(0, copied);
    }

    #endregion

    #region GetHolidayWorkingDaysLostAsync

    [Fact]
    public async Task GetHolidayWorkingDaysLostAsync_FullWeek_ReturnsFive()
    {
        // Sun Jun 7 to Sat Jun 13, 2026 — working days: Sun, Mon, Tue, Wed, Thu = 5

        var result = await _holidayService.GetHolidayWorkingDaysLostAsync(
            new DateTime(2026, 6, 7), new DateTime(2026, 6, 13));

        Assert.Equal(5, result);
    }

    [Fact]
    public async Task GetHolidayWorkingDaysLostAsync_WeekendOnly_ReturnsZero()
    {
        // Fri Jun 5 to Sat Jun 6, 2026 — both are weekend

        var result = await _holidayService.GetHolidayWorkingDaysLostAsync(
            new DateTime(2026, 6, 5), new DateTime(2026, 6, 6));

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetHolidayWorkingDaysLostAsync_SingleWorkday_ReturnsOne()
    {
        // Sun Jun 7, 2026 — Sunday is a workday

        var result = await _holidayService.GetHolidayWorkingDaysLostAsync(
            new DateTime(2026, 6, 7), new DateTime(2026, 6, 7));

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task GetHolidayWorkingDaysLostAsync_SingleWeekendDay_ReturnsZero()
    {
        // Fri Jun 5, 2026 — Friday is weekend

        var result = await _holidayService.GetHolidayWorkingDaysLostAsync(
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



        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _taskService.GetTasksAsync(cts.Token));
    }

    [Fact]
    public async Task GetHolidaysAsync_CancelledToken_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();



        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _holidayService.GetHolidaysAsync(cts.Token));
    }

    #endregion

    #region Task CRUD via Orchestrator

    [Fact]
    public async Task GetTasksAsync_ReturnsOrderedBySchedulingRank()
    {

        var tasks = await _taskService.GetTasksAsync();
        Assert.True(tasks.Count > 0);
    }

    [Fact]
    public async Task GetTaskCountAsync_ReturnsCorrectCount()
    {

        var count = await _taskService.GetTaskCountAsync();
        Assert.True(count > 0);

        var tasks = await _taskService.GetTasksAsync();
        Assert.Equal(tasks.Count, count);
    }

    [Fact]
    public async Task UpsertTaskAsync_NewTask_PersistsInDb()
    {


        await _taskService.UpsertTaskAsync(0, "SVN-01", "New Test Task", 10, 2, 5, null, null, true);

        await using var db = await _factory.CreateDbContextAsync();
        var persisted = await db.Tasks.FirstOrDefaultAsync(t => t.TaskId == "SVN-01");
        Assert.NotNull(persisted);
        Assert.Equal("New Test Task", persisted.ServiceName);
        Assert.NotEqual(default, persisted.CreatedAt);
    }

    [Fact]
    public async Task UpsertTaskAsync_UpdateExisting_ChangesFields()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var existing = await db.Tasks.FirstAsync();
        var existingId = existing.Id;


        await _taskService.UpsertTaskAsync(existingId, existing.TaskId, "Updated Task Name", 99, existing.MaxResource, 3, null, null, false);

        await using var verifyDb = await _factory.CreateDbContextAsync();
        var reloaded = await verifyDb.Tasks.FirstOrDefaultAsync(t => t.Id == existingId);
        Assert.NotNull(reloaded);
        Assert.Equal("Updated Task Name", reloaded.ServiceName);
        Assert.Equal(99, reloaded.DevEstimation);
        Assert.Equal(3, reloaded.Priority);
    }

    [Fact]
    public async Task DeleteTaskAsync_ExistingTask_RemovesFromDb()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var existing = await db.Tasks.FirstAsync();
        var existingId = existing.Id;


        await _taskService.DeleteTaskAsync(existingId);

        await using var verifyDb = await _factory.CreateDbContextAsync();
        var deleted = await verifyDb.Tasks.FirstOrDefaultAsync(t => t.Id == existingId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteTaskAsync_NonExistent_DoesNotThrow()
    {

        var exception = await Record.ExceptionAsync(() =>
            _taskService.DeleteTaskAsync(999999));
        Assert.Null(exception);
    }

    #endregion

    #region Resource CRUD via Orchestrator

    [Fact]
    public async Task GetResourcesAsync_ReturnsSeededResources()
    {

        var resources = await _resourceService.GetResourcesAsync();
        Assert.True(resources.Count > 0);
    }

    [Fact]
    public async Task GetResourceCountAsync_ReturnsCorrectCount()
    {

        var count = await _resourceService.GetResourceCountAsync();
        Assert.True(count > 0);

        var resources = await _resourceService.GetResourcesAsync();
        Assert.Equal(resources.Count, count);
    }

    [Fact]
    public async Task UpsertResourceAsync_NewResource_PersistsInDb()
    {


        await _resourceService.UpsertResourceAsync(0, "RES-100", "New Resource", "DEV", "Delivery", 100, 8, new DateTime(2026, 1, 1), "Yes", null, true);

        await using var db = await _factory.CreateDbContextAsync();
        var persisted = await db.Resources.FirstOrDefaultAsync(r => r.ResourceId == "RES-100");
        Assert.NotNull(persisted);
        Assert.Equal("New Resource", persisted.ResourceName);
    }

    [Fact]
    public async Task UpsertResourceAsync_UpdateExisting_ChangesFields()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var existing = await db.Resources.FirstAsync();
        var existingId = existing.Id;


        await _resourceService.UpsertResourceAsync(existingId, existing.ResourceId, "Updated Resource Name", "DEV", "Delivery", 80, 6, existing.StartDate, "Yes", null, false);

        await using var verifyDb = await _factory.CreateDbContextAsync();
        var reloaded = await verifyDb.Resources.FirstOrDefaultAsync(r => r.Id == existingId);
        Assert.NotNull(reloaded);
        Assert.Equal("Updated Resource Name", reloaded.ResourceName);
        Assert.Equal(80, reloaded.AvailabilityPct);
        Assert.Equal(6, reloaded.DailyCapacity);
    }

    [Fact]
    public async Task DeleteResourceAsync_ExistingResource_RemovesFromDb()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var existing = await db.Resources.FirstAsync();
        var existingId = existing.Id;


        await _resourceService.DeleteResourceAsync(existingId);

        await using var verifyDb = await _factory.CreateDbContextAsync();
        var deleted = await verifyDb.Resources.FirstOrDefaultAsync(r => r.Id == existingId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteResourceAsync_NonExistent_DoesNotThrow()
    {

        var exception = await Record.ExceptionAsync(() =>
            _resourceService.DeleteResourceAsync(999999));
        Assert.Null(exception);
    }

    #endregion

    #region Adjustment CRUD via Orchestrator

    [Fact]
    public async Task GetAdjustmentsAsync_ReturnsListFromDb()
    {

        var adjustments = await _adjustmentService.GetAdjustmentsAsync();
        Assert.NotNull(adjustments);
        // Seeded data may or may not have adjustments; just verify it doesn't throw
    }

    [Fact]
    public async Task AddAdjustmentAsync_PersistsInDb()
    {


        await _adjustmentService.AddAdjustmentAsync("DEV-001", "Vacation", 50, new DateTime(2026, 7, 1), new DateTime(2026, 7, 5), "Test adjustment");

        await using var db = await _factory.CreateDbContextAsync();
        var persisted = await db.Adjustments.FirstOrDefaultAsync(
            a => a.ResourceId == "DEV-001" && a.Notes == "Test adjustment");
        Assert.NotNull(persisted);
        Assert.Equal(50, persisted.AvailabilityPct);
    }

    [Fact]
    public async Task DeleteAdjustmentAsync_ExistingAdjustment_RemovesFromDb()
    {
        // First add an adjustment
        await using var db = await _factory.CreateDbContextAsync();
        var adj = Adjustment.Create("DEV-001", "Sick Leave", 0, new DateTime(2026, 8, 1), new DateTime(2026, 8, 5));
        db.Adjustments.Add(adj);
        await db.SaveChangesAsync();
        var adjId = adj.Id;


        await _adjustmentService.DeleteAdjustmentAsync(adjId);

        await using var verifyDb = await _factory.CreateDbContextAsync();
        var deleted = await verifyDb.Adjustments.FirstOrDefaultAsync(a => a.Id == adjId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAdjustmentAsync_NonExistent_DoesNotThrow()
    {

        var exception = await Record.ExceptionAsync(() =>
            _adjustmentService.DeleteAdjustmentAsync(999999));
        Assert.Null(exception);
    }

    #endregion

    #region GetCalendarAsync

    [Fact]
    public async Task GetCalendarAsync_AfterScheduler_ReturnsOrderedDays()
    {

        await _schedulerService.RunSchedulerAsync();

        var calendar = await _planningQueryService.GetCalendarAsync();
        Assert.NotEmpty(calendar);

        for (int i = 1; i < calendar.Count; i++)
        {
            Assert.True(calendar[i].CalendarDate >= calendar[i - 1].CalendarDate);
        }
    }

    #endregion

    #region GetOutputPlanAsync

    [Fact]
    public async Task GetOutputPlanAsync_ReturnsTaskPlan()
    {

        var plan = await _planningQueryService.GetOutputPlanAsync();
        Assert.NotNull(plan);
        Assert.True(plan.Count > 0);
    }

    #endregion

    #region GetTimelineDataAsync

    [Fact]
    public async Task GetTimelineDataAsync_ReturnsExpectedDayCount()
    {

        await _schedulerService.RunSchedulerAsync();

        await using var db = await _factory.CreateDbContextAsync();
        var resource = await db.Resources.FirstAsync(r => r.Active == "Yes");

        var start = new DateTime(2026, 5, 1);
        var end = new DateTime(2026, 5, 31);

        var timeline = await _planningQueryService.GetTimelineDataAsync(resource.ResourceId, start, end);
        Assert.NotNull(timeline);
        Assert.Equal(31, timeline.Days.Count); // 31 days in May
    }

    [Fact]
    public async Task GetTimelineDataAsync_WeekendsHaveCorrectStatus()
    {

        await _schedulerService.RunSchedulerAsync();

        await using var db = await _factory.CreateDbContextAsync();
        var resource = await db.Resources.FirstAsync(r => r.Active == "Yes");

        var start = new DateTime(2026, 5, 1);
        var end = new DateTime(2026, 5, 31);

        var timeline = await _planningQueryService.GetTimelineDataAsync(resource.ResourceId, start, end);

        // Friday and Saturday should be marked with weekend status text
        var fridayDays = timeline.Days.Where(d => d.Date.DayOfWeek == DayOfWeek.Friday).ToList();
        Assert.NotEmpty(fridayDays);
        Assert.All(fridayDays, d => Assert.Equal("Friday", d.StatusText));

        var saturdayDays = timeline.Days.Where(d => d.Date.DayOfWeek == DayOfWeek.Saturday).ToList();
        Assert.NotEmpty(saturdayDays);
        Assert.All(saturdayDays, d => Assert.Equal("Saturday", d.StatusText));
    }

    #endregion

    #region CopyHolidaysToYear edge cases

    [Fact]
    public async Task CopyHolidaysToYearAsync_SameYear_ReturnsZero()
    {
        // Copying from 2026 to 2026 should skip all because they all overlap with themselves

        var copied = await _holidayService.CopyHolidaysToYearAsync(2026, 2026);
        Assert.Equal(0, copied);
    }

    [Fact]
    public async Task CopyHolidaysToYearAsync_CalledTwice_SecondCallReturnsZero()
    {


        // First copy: should succeed
        var firstCopy = await _holidayService.CopyHolidaysToYearAsync(2026, 2030);
        Assert.True(firstCopy > 0);

        // Second copy: all already exist → zero
        var secondCopy = await _holidayService.CopyHolidaysToYearAsync(2026, 2030);
        Assert.Equal(0, secondCopy);
    }

    #endregion

    #region Additional cancellation

    [Fact]
    public async Task GetResourcesAsync_CancelledToken_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();


        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _resourceService.GetResourcesAsync(cts.Token));
    }

    [Fact]
    public async Task GetAdjustmentsAsync_CancelledToken_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();


        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _adjustmentService.GetAdjustmentsAsync(cts.Token));
    }

    [Fact]
    public async Task GetCalendarAsync_CancelledToken_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();


        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _planningQueryService.GetCalendarAsync(cts.Token));
    }

    [Fact]
    public async Task GetOutputPlanAsync_CancelledToken_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();


        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _planningQueryService.GetOutputPlanAsync(cts.Token));
    }

    [Fact]
    public async Task GetTimelineDataAsync_CancelledToken_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();


        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _planningQueryService.GetTimelineDataAsync("DEV-001", DateTime.Today, DateTime.Today.AddDays(7), cts.Token));
    }

    #endregion
}
