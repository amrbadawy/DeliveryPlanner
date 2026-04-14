using MediatR;
using NetArchTest.Rules;
using System.Collections;
using System.Reflection;

namespace SoftwareDeliveryPlanner.ArchTests;

public class DependencyRulesTests
{
    [Fact]
    public void Domain_Does_Not_Depend_On_Application()
    {
        var result = Types
            .InAssembly(typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("SoftwareDeliveryPlanner.Application")
            .GetResult();

        Assert.True(result.IsSuccessful, BuildFailureMessage(result.FailingTypeNames));
    }

    [Fact]
    public void Domain_Does_Not_Depend_On_Infrastructure()
    {
        var result = Types
            .InAssembly(typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("SoftwareDeliveryPlanner.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, BuildFailureMessage(result.FailingTypeNames));
    }

    [Fact]
    public void Domain_Does_Not_Depend_On_Web()
    {
        var result = Types
            .InAssembly(typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("SoftwareDeliveryPlanner.Web")
            .GetResult();

        Assert.True(result.IsSuccessful, BuildFailureMessage(result.FailingTypeNames));
    }

    [Fact]
    public void Application_Does_Not_Depend_On_Infrastructure()
    {
        var result = Types
            .InAssembly(typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("SoftwareDeliveryPlanner.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, BuildFailureMessage(result.FailingTypeNames));
    }

    [Fact]
    public void Application_Does_Not_Depend_On_Web()
    {
        var result = Types
            .InAssembly(typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("SoftwareDeliveryPlanner.Web")
            .GetResult();

        Assert.True(result.IsSuccessful, BuildFailureMessage(result.FailingTypeNames));
    }

    [Fact]
    public void Infrastructure_Does_Not_Depend_On_Web()
    {
        var result = Types
            .InAssembly(typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("SoftwareDeliveryPlanner.Web")
            .GetResult();

        Assert.True(result.IsSuccessful, BuildFailureMessage(result.FailingTypeNames));
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: No enums — use LookupValue entities
    // ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Domain")]
    [InlineData("Application")]
    [InlineData("Infrastructure")]
    public void No_Enums_Allowed_In_Project(string layer)
    {
        var assembly = layer switch
        {
            "Domain" => typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly,
            "Application" => typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly,
            "Infrastructure" => typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly,
            _ => throw new ArgumentException($"Unknown layer: {layer}")
        };

        var enums = assembly.GetTypes()
            .Where(t => t.IsEnum && t.IsPublic)
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(
            enums.Count == 0,
            $"Enums are forbidden (use LookupValue entities instead). " +
            $"Found in {layer}: {string.Join(", ", enums)}");
    }

    private static string BuildFailureMessage(IReadOnlyCollection<string> failingTypes)
    {
        if (failingTypes is { Count: > 0 })
        {
            return $"Violating types:{Environment.NewLine}{string.Join(Environment.NewLine, failingTypes.OrderBy(x => x))}";
        }

        if (failingTypes is IEnumerable enumerable)
        {
            var collected = new List<string>();
            foreach (var item in enumerable)
            {
                if (item is not null)
                {
                    collected.Add(item.ToString() ?? string.Empty);
                }
            }

            if (collected.Count > 0)
            {
                return $"Violating types:{Environment.NewLine}{string.Join(Environment.NewLine, collected.OrderBy(x => x))}";
            }
        }

        return string.Empty;
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Handler naming convention
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Handlers_Should_Have_Handler_Suffix()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        // Find all types implementing IRequestHandler<,> — they should end in "Handler"
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequestHandler<,>)))
            .ToList();

        Assert.NotEmpty(handlerTypes);

        var violators = handlerTypes
            .Where(t => !t.Name.EndsWith("Handler"))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(violators.Count == 0,
            $"All MediatR handlers must end with 'Handler'. Violating types: {string.Join(", ", violators)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Handlers should be sealed
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Handlers_Should_Be_Sealed()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequestHandler<,>)))
            .ToList();

        Assert.NotEmpty(handlerTypes);

        var unsealed = handlerTypes
            .Where(t => !t.IsSealed)
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(unsealed.Count == 0,
            $"All MediatR handlers must be sealed. Unsealed: {string.Join(", ", unsealed)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: No enums in Web project
    // Note: Web assembly requires ASP.NET Core runtime which
    // is not available in the test runner. The enum ban for
    // Domain/Application/Infrastructure is enforced by the
    // No_Enums_Allowed_In_Project test above.
    // ─────────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Validators follow naming convention
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Validators_Should_Have_Validator_Suffix()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        var validatorTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.BaseType != null && t.BaseType.IsGenericType
                && t.BaseType.GetGenericTypeDefinition().FullName != null
                && t.BaseType.GetGenericTypeDefinition().FullName.Contains("AbstractValidator"))
            .ToList();

        if (validatorTypes.Count == 0) return;

        var violators = validatorTypes
            .Where(t => !t.Name.EndsWith("Validator"))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(violators.Count == 0,
            $"All validators must end with 'Validator'. Violating: {string.Join(", ", violators)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Command/Query naming conventions
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Commands_Should_End_With_Command()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        // Find types implementing IRequest that are in Commands namespaces
        var commandTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace != null
                && t.Namespace.Contains("Commands")
                && t.GetInterfaces().Any(i =>
                    i == typeof(MediatR.IRequest<MediatR.Unit>)
                    || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>))))
            .ToList();

        if (commandTypes.Count == 0) return;

        var violators = commandTypes
            .Where(t => !t.Name.EndsWith("Command"))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(violators.Count == 0,
            $"All commands must end with 'Command'. Violating: {string.Join(", ", violators)}");
    }

    [Fact]
    public void Queries_Should_End_With_Query()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        var queryTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace != null
                && t.Namespace.Contains("Queries")
                && t.GetInterfaces().Any(i =>
                    i == typeof(MediatR.IRequest<MediatR.Unit>)
                    || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>))))
            .ToList();

        if (queryTypes.Count == 0) return;

        var violators = queryTypes
            .Where(t => !t.Name.EndsWith("Query"))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(violators.Count == 0,
            $"All queries must end with 'Query'. Violating: {string.Join(", ", violators)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Domain models should not reference EF Core
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Domain_Does_Not_Depend_On_EntityFramework()
    {
        var result = Types
            .InAssembly(typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, BuildFailureMessage(result.FailingTypeNames));
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Application layer must not reference EF Core
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Application_Does_Not_Depend_On_EntityFramework()
    {
        var result = Types
            .InAssembly(typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, BuildFailureMessage(result.FailingTypeNames));
    }
}
