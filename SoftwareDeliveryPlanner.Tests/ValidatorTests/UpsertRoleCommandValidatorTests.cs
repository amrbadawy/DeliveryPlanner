using FluentValidation.TestHelper;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Application.Roles.Commands;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Tests.ValidatorTests;

public class UpsertRoleCommandValidatorTests
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

    private static UpsertRoleCommand Valid(bool isNew = true) =>
        new(
            Id: isNew ? 0 : 1,
            Code: "DEV",
            DisplayName: "Developer",
            IsActive: true,
            SortOrder: 1,
            IsNew: isNew);

    [Fact]
    public async Task ValidCommand_PassesValidation()
    {
        var orchestrator = new StubRoleOrchestrator();
        var validator = new UpsertRoleCommandValidator(orchestrator);

        var result = await validator.TestValidateAsync(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task DuplicateCode_FailsValidation()
    {
        var orchestrator = new StubRoleOrchestrator
        {
            Roles = [new Role { Id = 2, Code = "DEV", DisplayName = "Developer", IsActive = true, SortOrder = 1 }]
        };
        var validator = new UpsertRoleCommandValidator(orchestrator);

        var result = await validator.TestValidateAsync(Valid());

        result.ShouldHaveValidationErrorFor(c => c.Code);
    }

    [Fact]
    public async Task DeactivateInUseRole_FailsValidation()
    {
        var orchestrator = new StubRoleOrchestrator
        {
            InUse = ["DEV"]
        };
        var validator = new UpsertRoleCommandValidator(orchestrator);

        var result = await validator.TestValidateAsync(Valid() with { IsActive = false });

        result.ShouldHaveValidationErrorFor(c => c.IsActive);
    }
}
