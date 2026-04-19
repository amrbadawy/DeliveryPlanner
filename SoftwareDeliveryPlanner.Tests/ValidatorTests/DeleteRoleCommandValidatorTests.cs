using FluentValidation.TestHelper;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Application.Roles.Commands;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Tests.ValidatorTests;

public class DeleteRoleCommandValidatorTests
{
    private sealed class StubRoleOrchestrator : IRoleOrchestrator
    {
        public List<Role> Roles { get; set; } = new();
        public HashSet<string> InUse { get; set; } = new(StringComparer.Ordinal);

        public Task<List<Role>> GetRolesAsync(bool includeInactive = true, CancellationToken cancellationToken = default)
            => Task.FromResult(Roles.ToList());

        public Task UpsertRoleAsync(int id, string code, string displayName, bool isActive, int sortOrder, bool isNew, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteRoleAsync(int id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> RoleCodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Roles.Any(r => r.Code == code && (!excludeId.HasValue || r.Id != excludeId.Value)));

        public Task<bool> IsRoleInUseAsync(string code, CancellationToken cancellationToken = default)
            => Task.FromResult(InUse.Contains(code));
    }

    [Fact]
    public async Task ValidDelete_WhenRoleNotInUse_PassesValidation()
    {
        var orchestrator = new StubRoleOrchestrator
        {
            Roles = [new Role { Id = 1, Code = "Developer", DisplayName = "Developer", IsActive = true, SortOrder = 1 }]
        };

        var validator = new DeleteRoleCommandValidator(orchestrator);
        var result = await validator.TestValidateAsync(new DeleteRoleCommand(1));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Delete_WhenRoleInUse_FailsValidation()
    {
        var orchestrator = new StubRoleOrchestrator
        {
            Roles = [new Role { Id = 1, Code = "Developer", DisplayName = "Developer", IsActive = true, SortOrder = 1 }],
            InUse = ["Developer"]
        };

        var validator = new DeleteRoleCommandValidator(orchestrator);
        var result = await validator.TestValidateAsync(new DeleteRoleCommand(1));

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }
}
