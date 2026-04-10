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
