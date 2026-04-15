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
                && t.BaseType.GetGenericTypeDefinition().FullName!.Contains("AbstractValidator"))
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

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Every command with input must have a validator
    // Commands with no constructor parameters (e.g. RunSchedulerCommand)
    // are excluded — there is nothing to validate.
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Every_Command_With_Parameters_Must_Have_A_Validator()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        // Commands that carry at least one input parameter
        var commandsWithInput = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Command"))
            .Where(t => t.GetConstructors().Any(c => c.GetParameters().Length > 0))
            .ToList();

        // Collect the validated types from all AbstractValidator<T> subclasses
        var validatedTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.BaseType is { IsGenericType: true }
                && t.BaseType.GetGenericTypeDefinition().FullName!.Contains("AbstractValidator"))
            .Select(t => t.BaseType!.GetGenericArguments()[0])
            .ToHashSet();

        var missing = commandsWithInput
            .Where(c => !validatedTypes.Contains(c))
            .Select(c => c.FullName ?? c.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(missing.Count == 0,
            $"Every command with input parameters must have a FluentValidation validator. " +
            $"Missing:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Domain exceptions must inherit DomainException
    // Prevents raw Exception/InvalidOperationException from leaking
    // out of the domain layer and breaking the exception hierarchy.
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Domain_Exceptions_Must_Inherit_DomainException()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly;
        var domainExceptionType = typeof(SoftwareDeliveryPlanner.Domain.SharedKernel.DomainException);

        var violations = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => typeof(Exception).IsAssignableFrom(t))
            .Where(t => t != domainExceptionType)                    // exclude DomainException itself
            .Where(t => !domainExceptionType.IsAssignableFrom(t))    // must inherit from it
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(violations.Count == 0,
            $"All domain exception classes must inherit from DomainException. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: All public interfaces must start with "I"
    // ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Domain")]
    [InlineData("Application")]
    [InlineData("Infrastructure")]
    public void Interfaces_Must_Start_With_I(string layer)
    {
        var assembly = layer switch
        {
            "Domain"         => typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly,
            "Application"    => typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly,
            "Infrastructure" => typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly,
            _                => throw new ArgumentException($"Unknown layer: {layer}")
        };

        var violations = assembly.GetTypes()
            .Where(t => t.IsInterface && t.IsPublic && !t.Name.StartsWith("I"))
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(violations.Count == 0,
            $"All public interfaces in {layer} must start with 'I'. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Domain must have zero third-party NuGet dependencies
    // Only Microsoft/System BCL assemblies and solution-internal assemblies
    // are permitted as references.
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Domain_Must_Have_No_Third_Party_Dependencies()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly;

        var allowedPrefixes = new[]
        {
            "System",
            "Microsoft",
            "mscorlib",
            "netstandard",
            "SoftwareDeliveryPlanner",
        };

        var thirdPartyRefs = assembly.GetReferencedAssemblies()
            .Where(a => !allowedPrefixes.Any(prefix =>
                a.Name!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .Select(a => a.FullName)
            .OrderBy(n => n)
            .ToList();

        Assert.True(thirdPartyRefs.Count == 0,
            $"Domain must not reference third-party assemblies. " +
            $"Found:{Environment.NewLine}{string.Join(Environment.NewLine, thirdPartyRefs)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Queries must not return Unit
    // A query that returns nothing is a command — mixing this
    // breaks the CQS principle and creates hidden side-effect chains.
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Queries_Must_Not_Return_Unit()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        var queryTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace != null
                && t.Namespace.Contains("Queries")
                && t.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>)))
            .ToList();

        var violations = queryTypes
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>)
                && i.GetGenericArguments()[0] == typeof(MediatR.Unit)))
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(violations.Count == 0,
            $"Queries must return meaningful data, not Unit. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Handlers must live in the same namespace
    // as the request they handle. Cross-namespace handlers indicate
    // feature creep and obscure ownership.
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Handlers_Must_Be_In_Same_Namespace_As_Their_Request()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequestHandler<,>)))
            .ToList();

        var violations = new List<string>();
        foreach (var handler in handlerTypes)
        {
            var iface = handler.GetInterfaces().First(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequestHandler<,>));
            var requestType = iface.GetGenericArguments()[0];

            if (handler.Namespace != requestType.Namespace)
            {
                violations.Add(
                    $"{handler.Name} (ns: {handler.Namespace}) " +
                    $"handles {requestType.Name} (ns: {requestType.Namespace})");
            }
        }

        Assert.True(violations.Count == 0,
            $"Handlers must live in the same namespace as their request. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Domain value objects must not have public setters
    // Domain model entities (TaskItem, TeamMember, etc.) are EF Core–mapped
    // and their encapsulation is enforced via domain factory Create() methods.
    // Value objects in SharedKernel must be strictly immutable.
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Domain_ValueObjects_Must_Not_Have_Public_Setters()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly;

        var valueObjectTypes = assembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.Contains("ValueObjects"))
            .ToList();

        var violations = new List<string>();
        foreach (var type in valueObjectTypes)
        {
            var publicSetters = type.GetProperties()
                .Where(p => p.SetMethod != null && p.SetMethod.IsPublic)
                .Select(p => $"{type.Name}.{p.Name}");
            violations.AddRange(publicSetters);
        }

        Assert.True(violations.Count == 0,
            $"Domain value objects must not have public setters — they must be immutable. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }
}
