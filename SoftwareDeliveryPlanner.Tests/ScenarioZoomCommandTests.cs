using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Application.Scenarios.Commands;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Tests;

public class SetScenarioZoomLevelCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenScenarioNotFound_ReturnsNotFound()
    {
        var orchestrator = new StubScenarioOrchestrator { SetResult = null };
        var handler = new SetScenarioZoomLevelCommandHandler(orchestrator);

        var result = await handler.Handle(new SetScenarioZoomLevelCommand(999, "MONTH"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenScenarioExists_ReturnsSuccess()
    {
        var scenario = PlanScenario.Create("Scenario", 1, 1, 0, 0, 0, null, null, 1, null, DateTime.UtcNow);
        var orchestrator = new StubScenarioOrchestrator { SetResult = scenario };
        var handler = new SetScenarioZoomLevelCommandHandler(orchestrator);

        var result = await handler.Handle(new SetScenarioZoomLevelCommand(1, "MONTH"), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    private sealed class StubScenarioOrchestrator : IScenarioOrchestrator
    {
        public PlanScenario? SetResult { get; set; }

        public Task<List<PlanScenario>> GetScenariosAsync() => Task.FromResult(new List<PlanScenario>());

        public Task<PlanScenario?> GetScenarioWithSnapshotsAsync(int id) => Task.FromResult<PlanScenario?>(null);

        public Task<PlanScenario?> SetScenarioZoomLevelAsync(int id, string? zoomLevel, CancellationToken cancellationToken = default)
            => Task.FromResult(SetResult);

        public Task SaveScenarioAsync(PlanScenario scenario) => Task.CompletedTask;

        public Task SaveScenarioWithSnapshotsAsync(PlanScenario scenario, List<TaskItem> tasks) => Task.CompletedTask;

        public Task DeleteScenarioAsync(int id) => Task.CompletedTask;
    }
}
