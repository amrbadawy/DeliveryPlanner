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

    [Fact]
    public void MarkAsDefault_SetsFlag()
    {
        var view = SavedView.Create("Initial", "tasks", "{\"search\":\"x\"}");

        view.MarkAsDefault();

        Assert.True(view.IsDefault);
    }

    [Fact]
    public void ClearDefault_ClearsFlag()
    {
        var view = SavedView.Create("Initial", "tasks", "{\"search\":\"x\"}");
        view.MarkAsDefault();

        view.ClearDefault();

        Assert.False(view.IsDefault);
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

    [Fact]
    public async Task Handle_DefaultToggle_SetsDefaultFlag()
    {
        var existing = SavedView.Create("View A", "tasks", "{\"status\":[\"NOT_STARTED\"]}", "owner-a");
        var updated = SavedView.Create("View A", "tasks", "{\"status\":[\"NOT_STARTED\"]}", "owner-a");
        updated.MarkAsDefault();

        var orchestrator = new StubSavedViewOrchestrator
        {
            Existing = existing,
            SetDefaultResult = updated
        };

        var handler = new SetDefaultSavedViewCommandHandler(orchestrator);

        var result = await handler.Handle(new SetDefaultSavedViewCommand(123, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsDefault);
    }

    [Fact]
    public async Task Handle_DefaultToggle_NotFound_ReturnsNotFound()
    {
        var orchestrator = new StubSavedViewOrchestrator
        {
            Existing = null,
            SetDefaultResult = null
        };

        var handler = new SetDefaultSavedViewCommandHandler(orchestrator);

        var result = await handler.Handle(new SetDefaultSavedViewCommand(999, true), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
    }

    private sealed class StubSavedViewOrchestrator : ISavedViewOrchestrator
    {
        public SavedView? Existing { get; set; }
        public SavedView? RenameResult { get; set; }
        public SavedView? SetDefaultResult { get; set; }

        public Task<List<SavedView>> ListAsync(string pageKey, string? ownerKey, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<SavedView>());

        public Task<SavedView?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(Existing);

        public Task<SavedView?> GetDefaultAsync(string pageKey, string? ownerKey, CancellationToken cancellationToken = default)
            => Task.FromResult<SavedView?>(null);

        public Task<SavedView> UpsertAsync(string name, string pageKey, string payloadJson, string? ownerKey, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<SavedView?> RenameAsync(int id, string name, CancellationToken cancellationToken = default)
            => Task.FromResult(RenameResult);

        public Task<SavedView?> SetDefaultAsync(int id, bool isDefault, CancellationToken cancellationToken = default)
            => Task.FromResult(SetDefaultResult);

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
