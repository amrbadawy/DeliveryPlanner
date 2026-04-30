using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Application.SavedViews.Commands;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Tests;

public class SavedViewDomainTests
{
    [Fact]
    public void Rename_ValidName_UpdatesName()
    {
        var view = SavedView.Create("Initial", "tasks", "{\"search\":\"x\"}");

        view.Rename("Renamed");

        Assert.Equal("Renamed", view.Name);
    }
}

public class RenameSavedViewCommandHandlerTests
{
    [Fact]
    public async Task Handle_DuplicateName_ReturnsConflict()
    {
        var orchestrator = new StubSavedViewOrchestrator
        {
            Existing = SavedView.Create("View A", "tasks", "{\"status\":[\"NOT_STARTED\"]}", "owner-a"),
            RenameResult = null
        };

        var handler = new RenameSavedViewCommandHandler(orchestrator);

        var result = await handler.Handle(new RenameSavedViewCommand(123, "View B"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict", result.Error.Code);
    }

    private sealed class StubSavedViewOrchestrator : ISavedViewOrchestrator
    {
        public SavedView? Existing { get; set; }
        public SavedView? RenameResult { get; set; }

        public Task<List<SavedView>> ListAsync(string pageKey, string? ownerKey, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<SavedView>());

        public Task<SavedView?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(Existing);

        public Task<SavedView> UpsertAsync(string name, string pageKey, string payloadJson, string? ownerKey, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<SavedView?> RenameAsync(int id, string name, CancellationToken cancellationToken = default)
            => Task.FromResult(RenameResult);

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
