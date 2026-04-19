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

    // DTO-associated semantic enums that are explicitly permitted.
    // These represent presentation/contract concepts in DTOs, not
    // domain-level lookup data that belongs in a LookupValue table.
    private static readonly HashSet<string> AllowedDtoEnums = new(StringComparer.Ordinal)
    {
        "SoftwareDeliveryPlanner.Application.Abstractions.TimelineDayStatus",
    };

    [Theory]
    [InlineData("Domain")]
    [InlineData("Application")]
    [InlineData("Infrastructure")]
    [InlineData("SharedKernel")]
    public void No_Enums_Allowed_In_Project(string layer)
    {
        var assembly = layer switch
        {
            "Domain" => typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly,
            "Application" => typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly,
            "Infrastructure" => typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly,
            "SharedKernel" => typeof(SoftwareDeliveryPlanner.SharedKernel.AssemblyMarker).Assembly,
            _ => throw new ArgumentException($"Unknown layer: {layer}")
        };

        var enums = assembly.GetTypes()
            .Where(t => t.IsEnum && t.IsPublic)
            .Where(t => !AllowedDtoEnums.Contains(t.FullName ?? t.Name))
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
        var domainExceptionType = typeof(SoftwareDeliveryPlanner.SharedKernel.DomainException);

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
    [InlineData("SharedKernel")]
    public void Interfaces_Must_Start_With_I(string layer)
    {
        var assembly = layer switch
        {
            "Domain"         => typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly,
            "Application"    => typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly,
            "Infrastructure" => typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly,
            "SharedKernel"   => typeof(SoftwareDeliveryPlanner.SharedKernel.AssemblyMarker).Assembly,
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

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Commands must be record types
    // Records are immutable by design — a class-based command can
    // be mutated between pipeline behaviors, silently corrupting state.
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Commands_Must_Be_Record_Types()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        var commandTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Command"))
            .ToList();

        // C# records compile to classes with a compiler-generated <Clone>$ method.
        var violations = commandTypes
            .Where(t => t.GetMethod("<Clone>$") == null)
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(violations.Count == 0,
            $"All commands must be 'record' types, not plain classes. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Queries must be record types
    // Same reasoning as commands — immutable intent objects.
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Queries_Must_Be_Record_Types()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        var queryTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Query"))
            .ToList();

        var violations = queryTypes
            .Where(t => t.GetMethod("<Clone>$") == null)
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(violations.Count == 0,
            $"All queries must be 'record' types, not plain classes. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Validators must be sealed
    // Open validators allow inheritance-based composition which
    // produces unpredictable validation chains. Parallel to the
    // handler sealed rule.
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Validators_Must_Be_Sealed()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        var validators = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.BaseType is { IsGenericType: true }
                && t.BaseType.GetGenericTypeDefinition().FullName!.Contains("AbstractValidator"))
            .ToList();

        var violations = validators
            .Where(t => !t.IsSealed)
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(violations.Count == 0,
            $"All validators must be sealed. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Pipeline behaviors must be sealed
    // Unsealed behaviors invite inheritance which produces
    // unpredictable pipeline execution order and side effects.
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Pipeline_Behaviors_Must_Be_Sealed()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        var behaviors = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(MediatR.IPipelineBehavior<,>)))
            .ToList();

        var violations = behaviors
            .Where(t => !t.IsSealed)
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(violations.Count == 0,
            $"All MediatR pipeline behaviors must be sealed. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Every DbSet<T> must have an explicit
    // IEntityTypeConfiguration<T> in Infrastructure.
    // Without this, EF Core silently falls back to convention-based
    // mapping, producing wrong column types or missing indexes in prod.
    // Uses string-based type matching to avoid adding an EF Core
    // dependency to this architecture test project.
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Every_DbSet_Must_Have_Explicit_Configuration()
    {
        var infraAssembly = typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly;

        const string dbSetFullName        = "Microsoft.EntityFrameworkCore.DbSet`1";
        const string dbContextFullName    = "Microsoft.EntityFrameworkCore.DbContext";
        const string entityConfigFullName = "Microsoft.EntityFrameworkCore.IEntityTypeConfiguration`1";

        // Find the primary (non-ReadOnly) DbContext subclass in Infrastructure
        var dbContextType = infraAssembly.GetTypes()
            .FirstOrDefault(t => t.IsClass && !t.IsAbstract
                && !t.Name.Contains("ReadOnly")
                && IsAssignableToName(t, dbContextFullName));

        Assert.NotNull(dbContextType);

        // Collect all entity types exposed as DbSet<T> properties
        var dbSetEntityTypes = dbContextType!.GetProperties()
            .Where(p => p.PropertyType.IsGenericType
                && p.PropertyType.GetGenericTypeDefinition().FullName == dbSetFullName)
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .ToHashSet();

        // Collect all entity types that have an explicit IEntityTypeConfiguration<T>
        var configuredEntityTypes = infraAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType
                    && i.GetGenericTypeDefinition().FullName == entityConfigFullName)
                .Select(i => i.GetGenericArguments()[0]))
            .ToHashSet();

        var missing = dbSetEntityTypes
            .Where(e => !configuredEntityTypes.Contains(e))
            .Select(e => e.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(missing.Count == 0,
            $"Every DbSet<T> in the DbContext must have an explicit IEntityTypeConfiguration<T>. " +
            $"Missing:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: EF entity configurations must only exist
    // in the Infrastructure layer. A configuration file in Domain
    // or Application would pull an EF Core dependency into the
    // wrong layer and break the persistence boundary.
    // ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Domain")]
    [InlineData("Application")]
    public void EF_Configurations_Must_Not_Exist_Outside_Infrastructure(string layer)
    {
        const string entityConfigFullName = "Microsoft.EntityFrameworkCore.IEntityTypeConfiguration`1";

        var assembly = layer switch
        {
            "Domain"      => typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly,
            "Application" => typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly,
            _             => throw new ArgumentException($"Unknown layer: {layer}")
        };

        var violations = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition().FullName == entityConfigFullName))
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(violations.Count == 0,
            $"IEntityTypeConfiguration<T> implementations must only exist in Infrastructure. " +
            $"Found in {layer}:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Public concrete classes in the Infrastructure
    // layer (namespace SoftwareDeliveryPlanner.Infrastructure.*) must
    // implement at least one interface from the Application layer.
    // This ensures the abstraction boundary is always owned by Application,
    // and no Infrastructure class is directly consumed without a contract.
    //
    // Excluded by design:
    //   - DbContext subclasses (persistence host, not a service)
    //   - IEntityTypeConfiguration<T> implementations (EF mapping, not services)
    //   - Design-time factories (*DesignTimeFactory, used only by EF tooling)
    //   - AssemblyMarker (marker type, no behaviour)
    // Note: SchedulingEngine lives in namespace SoftwareDeliveryPlanner.Services
    // (not .Infrastructure.*) and is an internal implementation detail of
    // SchedulingOrchestrator — it should be made internal in a future cleanup.
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Infrastructure_Services_Must_Implement_Application_Interface()
    {
        var infraAssembly = typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly;
        var appAssembly   = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        const string dbContextFullName    = "Microsoft.EntityFrameworkCore.DbContext";
        const string entityConfigFullName = "Microsoft.EntityFrameworkCore.IEntityTypeConfiguration`1";

        var appInterfaces = appAssembly.GetTypes()
            .Where(t => t.IsInterface)
            .ToHashSet();

        var serviceClasses = infraAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition && t.IsPublic)
            .Where(t => t.Namespace != null
                && t.Namespace.StartsWith("SoftwareDeliveryPlanner.Infrastructure",
                    StringComparison.Ordinal)
                && !t.Namespace.Contains("Migrations"))
            .Where(t => !IsAssignableToName(t, dbContextFullName))
            .Where(t => !t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition().FullName == entityConfigFullName))
            .Where(t => !t.Name.EndsWith("DesignTimeFactory"))
            .Where(t => t.Name != "AssemblyMarker")
            .ToList();

        var violations = serviceClasses
            .Where(t => !t.GetInterfaces().Any(i => appInterfaces.Contains(i)))
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(violations.Count == 0,
            $"All public Infrastructure service classes must implement an Application-layer interface. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: No async void methods in Domain or Application
    // async void cannot be awaited, exceptions are swallowed (crashing
    // the process in Blazor Server), and the method bypasses the
    // MediatR pipeline entirely. Always use async Task instead.
    // ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Domain")]
    [InlineData("Application")]
    [InlineData("SharedKernel")]
    public void No_Async_Void_Methods(string layer)
    {
        var assembly = layer switch
        {
            "Domain"       => typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly,
            "Application"  => typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly,
            "SharedKernel" => typeof(SoftwareDeliveryPlanner.SharedKernel.AssemblyMarker).Assembly,
            _              => throw new ArgumentException($"Unknown layer: {layer}")
        };

        var violations = assembly.GetTypes()
            .SelectMany(t => t.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.DeclaredOnly))
            .Where(m => m.ReturnType == typeof(void))
            .Where(m => m.GetCustomAttributes(
                typeof(System.Runtime.CompilerServices.AsyncStateMachineAttribute),
                inherit: false).Length > 0)
            .Select(m => $"{m.DeclaringType?.Name}.{m.Name}")
            .OrderBy(n => n)
            .ToList();

        Assert.True(violations.Count == 0,
            $"async void methods are forbidden in {layer} — use async Task instead. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Domain must not inject ILogger<T>
    // Domain objects are pure business logic — logging is an
    // infrastructure concern. ILogger injection drags a logging
    // abstraction into the domain, coupling it to the logging
    // infrastructure and making it harder to test.
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Domain_Must_Not_Inject_ILogger()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly;

        const string iLoggerGenericName = "Microsoft.Extensions.Logging.ILogger`1";

        var violations = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetConstructors().Any(c =>
                c.GetParameters().Any(p =>
                    p.ParameterType.IsGenericType &&
                    p.ParameterType.GetGenericTypeDefinition().FullName == iLoggerGenericName)))
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(violations.Count == 0,
            $"Domain classes must not inject ILogger<T> — logging is an infrastructure concern. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: No IServiceProvider injection in
    // Domain or Application (service locator anti-pattern).
    // Hidden dependencies make classes untestable, break the
    // dependency inversion principle, and obscure the true
    // dependency graph.
    // ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Domain")]
    [InlineData("Application")]
    [InlineData("SharedKernel")]
    public void No_IServiceProvider_Injection(string layer)
    {
        var assembly = layer switch
        {
            "Domain"       => typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly,
            "Application"  => typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly,
            "SharedKernel" => typeof(SoftwareDeliveryPlanner.SharedKernel.AssemblyMarker).Assembly,
            _              => throw new ArgumentException($"Unknown layer: {layer}")
        };

        const string serviceProviderFullName = "System.IServiceProvider";

        var violations = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetConstructors().Any(c =>
                c.GetParameters().Any(p =>
                    p.ParameterType.FullName == serviceProviderFullName)))
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(violations.Count == 0,
            $"IServiceProvider injection is forbidden in {layer} — it is a service locator anti-pattern. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Handlers must not directly inject
    // other handlers. Cross-handler calls must go through
    // ISender (MediatR pipeline) to preserve pipeline
    // behaviors (validation, logging, transactions, etc.).
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Handlers_Must_Not_Inject_Other_Handlers()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequestHandler<,>)))
            .ToHashSet();

        var violations = handlerTypes
            .Where(handler => handler.GetConstructors()
                .Any(c => c.GetParameters()
                    .Any(p => handlerTypes.Contains(p.ParameterType))))
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(violations.Count == 0,
            $"Handlers must not inject other handlers directly — use ISender to dispatch through the pipeline. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Application DTOs must be sealed record types.
    // DTOs cross layer boundaries as return values. Records
    // enforce value equality and immutability; sealed prevents
    // inheritance-based mutation of the DTO contract.
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Application_DTOs_Must_Be_Sealed_Record_Types()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        var dtoTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Dto"))
            .ToList();

        Assert.NotEmpty(dtoTypes);

        var violations = dtoTypes
            .Where(t => !t.IsSealed || t.GetMethod("<Clone>$") == null)
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(violations.Count == 0,
            $"All Application DTOs must be 'sealed record' types. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: No mutable static fields in Domain
    // or Application. Blazor Server shares one process across
    // all user sessions — mutable statics are shared state
    // that causes data leaks and race conditions between
    // concurrent user requests.
    // ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Domain")]
    [InlineData("Application")]
    [InlineData("SharedKernel")]
    public void No_Mutable_Static_Fields(string layer)
    {
        var assembly = layer switch
        {
            "Domain"       => typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly,
            "Application"  => typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly,
            "SharedKernel" => typeof(SoftwareDeliveryPlanner.SharedKernel.AssemblyMarker).Assembly,
            _              => throw new ArgumentException($"Unknown layer: {layer}")
        };

        var violations = assembly.GetTypes()
            .SelectMany(t => t.GetFields(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Where(f => !f.IsLiteral && !f.IsInitOnly)
            // Exclude fields with CompilerGeneratedAttribute (compiler-generated backing fields)
            .Where(f => !f.GetCustomAttributes(
                typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false).Any())
            // Exclude fields on compiler-generated types (e.g. <>c lambda cache classes, <>O delegate cache)
            .Where(f => f.DeclaringType?.GetCustomAttributes(
                typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false).Any() != true)
            .Select(f => $"{f.DeclaringType?.Name}.{f.Name}")
            .OrderBy(n => n)
            .ToList();

        Assert.True(violations.Count == 0,
            $"Mutable static fields are forbidden in {layer} — use dependency injection instead. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // Architecture Rule: Application and Infrastructure must
    // each contain a DependencyInjection static class.
    // Each layer is responsible for registering its own services.
    // Centralising registrations in the host is a dependency
    // inversion violation and makes layers non-portable.
    // ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Application")]
    [InlineData("Infrastructure")]
    public void Layer_Must_Have_DependencyInjection_Static_Class(string layer)
    {
        var assembly = layer switch
        {
            "Application"    => typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly,
            "Infrastructure" => typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly,
            _                => throw new ArgumentException($"Unknown layer: {layer}")
        };

        // In C#, 'static class' compiles to abstract + sealed in IL.
        var hasClass = assembly.GetTypes()
            .Any(t => t.Name == "DependencyInjection" && t.IsClass && t.IsAbstract && t.IsSealed);

        Assert.True(hasClass,
            $"{layer} must contain a 'DependencyInjection' static class to own its own service registrations.");
    }

    // ─────────────────────────────────────────────────────────
    // Wave 5: Enterprise Architecture Rules
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void EF_Configurations_Must_Be_Sealed()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly;

        var configTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition().FullName == "Microsoft.EntityFrameworkCore.IEntityTypeConfiguration`1"))
            .ToList();

        var unsealed = configTypes
            .Where(t => !t.IsSealed)
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(unsealed.Count == 0,
            $"All EF Core IEntityTypeConfiguration<T> implementations must be sealed. Unsealed: {string.Join(", ", unsealed)}");
    }

    [Fact]
    public void DbContext_Must_Be_Internal_To_Infrastructure()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly;

        var dbContextTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => IsAssignableToName(t, "Microsoft.EntityFrameworkCore.DbContext"))
            .ToList();

        var publicContexts = dbContextTypes
            .Where(t => t.IsPublic)
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(publicContexts.Count == 0,
            $"DbContext types must be internal to Infrastructure. Public contexts found: {string.Join(", ", publicContexts)}");
    }

    [Fact]
    public void Application_Handlers_Must_Be_Internal()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequestHandler<,>)))
            .ToList();

        var publicHandlers = handlerTypes
            .Where(t => t.IsPublic)
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(publicHandlers.Count == 0,
            $"MediatR IRequestHandler<,> implementations must be internal. Public handlers found: {string.Join(", ", publicHandlers)}");
    }

    [Fact]
    public void Queries_Must_Not_Return_Untyped_Dictionary()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        var queryTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>)))
            .ToList();

        var violatingQueries = new List<string>();

        foreach (var query in queryTypes)
        {
            var iface = query.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>));
            if (iface == null) continue;

            var returnType = iface.GenericTypeArguments[0];
            if (returnType.IsGenericType &&
                returnType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                violatingQueries.Add($"{query.FullName} -> {returnType.Name}");
            }
        }

        Assert.True(violatingQueries.Count == 0,
            $"Query handlers must not return untyped Dictionary<,>. Violating queries: {string.Join(", ", violatingQueries)}");
    }

    [Theory]
    [InlineData("Domain", "SoftwareDeliveryPlanner.Domain")]
    [InlineData("Application", "SoftwareDeliveryPlanner.Application")]
    [InlineData("Infrastructure", "SoftwareDeliveryPlanner.Infrastructure")]
    [InlineData("SharedKernel", "SoftwareDeliveryPlanner.SharedKernel")]
    public void Types_In_Assembly_Must_Use_Layer_Namespace(string layer, string expectedNamespace)
    {
        var assembly = layer switch
        {
            "Domain"         => typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly,
            "Application"    => typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly,
            "Infrastructure" => typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly,
            "SharedKernel"   => typeof(SoftwareDeliveryPlanner.SharedKernel.AssemblyMarker).Assembly,
            _                => throw new ArgumentException($"Unknown layer: {layer}")
        };

        var violatingTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
            .Where(t => !t.Namespace?.StartsWith(expectedNamespace, StringComparison.Ordinal) ?? true)
            .Select(t => $"{t.FullName} (namespace: {t.Namespace})")
            .ToList();

        Assert.True(violatingTypes.Count == 0,
            $"All public types in {layer} must be in the '{expectedNamespace}.*' namespace. " +
            $"Violating types:{Environment.NewLine}{string.Join(Environment.NewLine, violatingTypes)}");
    }

    [Theory]
    [InlineData("Domain")]
    [InlineData("Infrastructure")]
    public void DateTime_Now_Must_Not_Be_Used_In_Domain_Or_Infrastructure(string layer)
    {
        var basePath = layer switch
        {
            "Domain" => Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "SoftwareDeliveryPlanner.Domain"),
            "Infrastructure" => Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "SoftwareDeliveryPlanner.Infrastructure"),
            _ => throw new ArgumentException($"Unknown layer: {layer}")
        };

        basePath = Path.GetFullPath(basePath);

        if (!Directory.Exists(basePath))
        {
            basePath = layer switch
            {
                "Domain" => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SoftwareDeliveryPlanner.Domain"),
                "Infrastructure" => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SoftwareDeliveryPlanner.Infrastructure"),
                _ => throw new ArgumentException($"Unknown layer: {layer}")
            };
            basePath = Path.GetFullPath(basePath);
        }

        if (!Directory.Exists(basePath))
        {
            Assert.Fail($"Directory not found for {layer}: {basePath}");
            return;
        }

        var csFiles = Directory.GetFiles(basePath, "*.cs", SearchOption.AllDirectories);

        var violations = new List<string>();

        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);
            var lines = content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains("DateTime.Now") || line.Contains("DateTime.UtcNow"))
                {
                    var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
                    violations.Add($"{relativePath}:{i + 1} — {line.Trim()}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"DateTime.Now/DateTime.UtcNow is forbidden in {layer}. Use injected TimeProvider instead. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // SharedKernel Isolation Rules
    // SharedKernel sits at the bottom of the dependency graph.
    // It must not depend on any other solution layer —
    // Domain, Application, Infrastructure, or Web.
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void SharedKernel_Does_Not_Depend_On_Domain()
    {
        var result = Types
            .InAssembly(typeof(SoftwareDeliveryPlanner.SharedKernel.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("SoftwareDeliveryPlanner.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful, BuildFailureMessage(result.FailingTypeNames));
    }

    [Fact]
    public void SharedKernel_Does_Not_Depend_On_Application()
    {
        var result = Types
            .InAssembly(typeof(SoftwareDeliveryPlanner.SharedKernel.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("SoftwareDeliveryPlanner.Application")
            .GetResult();

        Assert.True(result.IsSuccessful, BuildFailureMessage(result.FailingTypeNames));
    }

    [Fact]
    public void SharedKernel_Does_Not_Depend_On_Infrastructure()
    {
        var result = Types
            .InAssembly(typeof(SoftwareDeliveryPlanner.SharedKernel.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("SoftwareDeliveryPlanner.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, BuildFailureMessage(result.FailingTypeNames));
    }

    [Fact]
    public void SharedKernel_Does_Not_Depend_On_Web()
    {
        var result = Types
            .InAssembly(typeof(SoftwareDeliveryPlanner.SharedKernel.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("SoftwareDeliveryPlanner.Web")
            .GetResult();

        Assert.True(result.IsSuccessful, BuildFailureMessage(result.FailingTypeNames));
    }

    [Fact]
    public void SharedKernel_Must_Have_No_Third_Party_Dependencies()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.SharedKernel.AssemblyMarker).Assembly;

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
            $"SharedKernel must not reference third-party assemblies. " +
            $"Found:{Environment.NewLine}{string.Join(Environment.NewLine, thirdPartyRefs)}");
    }

    // ─────────────────────────────────────────────────────────
    // ReadOnly DbContext Rules
    // ReadOnly DbContext types must be internal, must override
    // SaveChanges/SaveChangesAsync to prevent writes, and must
    // expose the same DbSet<T> properties as the primary context.
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void ReadOnly_DbContexts_Must_Be_Internal()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly;

        var readOnlyContexts = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Name.Contains("ReadOnly")
                && IsAssignableToName(t, "Microsoft.EntityFrameworkCore.DbContext"))
            .Where(t => t.IsPublic)
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(readOnlyContexts.Count == 0,
            $"ReadOnly DbContext types must be internal. " +
            $"Public: {string.Join(", ", readOnlyContexts)}");
    }

    [Fact]
    public void ReadOnly_DbContexts_Must_Override_SaveChanges()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly;

        var readOnlyContexts = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Name.Contains("ReadOnly")
                && IsAssignableToName(t, "Microsoft.EntityFrameworkCore.DbContext"))
            .ToList();

        Assert.NotEmpty(readOnlyContexts);

        foreach (var ctx in readOnlyContexts)
        {
            var saveChangesOverrides = ctx
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name == "SaveChanges" || m.Name == "SaveChangesAsync")
                .ToList();

            Assert.True(saveChangesOverrides.Count >= 2,
                $"{ctx.Name} must override both SaveChanges and SaveChangesAsync to prevent writes. " +
                $"Found {saveChangesOverrides.Count} override(s).");
        }
    }

    [Fact]
    public void ReadOnly_DbSets_Must_Match_Primary_DbContext()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly;

        const string dbSetFullName    = "Microsoft.EntityFrameworkCore.DbSet`1";
        const string dbContextName    = "Microsoft.EntityFrameworkCore.DbContext";

        var dbContextTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && IsAssignableToName(t, dbContextName))
            .ToList();

        var primaryContext = dbContextTypes
            .FirstOrDefault(t => !t.Name.Contains("ReadOnly") && !t.Name.Contains("DesignTime"));
        var readOnlyContexts = dbContextTypes
            .Where(t => t.Name.Contains("ReadOnly"))
            .ToList();

        Assert.NotNull(primaryContext);
        Assert.NotEmpty(readOnlyContexts);

        var primaryDbSets = primaryContext!.GetProperties()
            .Where(p => p.PropertyType.IsGenericType
                && p.PropertyType.GetGenericTypeDefinition().FullName == dbSetFullName)
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .ToHashSet();

        foreach (var roCtx in readOnlyContexts)
        {
            var roDbSets = roCtx.GetProperties()
                .Where(p => p.PropertyType.IsGenericType
                    && p.PropertyType.GetGenericTypeDefinition().FullName == dbSetFullName)
                .Select(p => p.PropertyType.GetGenericArguments()[0])
                .ToHashSet();

            var missing = primaryDbSets
                .Except(roDbSets)
                .Select(t => t.Name)
                .OrderBy(n => n)
                .ToList();

            Assert.True(missing.Count == 0,
                $"{roCtx.Name} is missing DbSet<T> for: {string.Join(", ", missing)}. " +
                $"Must match {primaryContext.Name}.");
        }
    }

    // ─────────────────────────────────────────────────────────
    // Phase 2B: Rich Domain Model Architecture Rules
    // ─────────────────────────────────────────────────────────

    // ── Aggregate Root Rules ─────────────────────────────────

    [Fact]
    public void Aggregate_Roots_Must_Inherit_AggregateRoot()
    {
        var domainAssembly = typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly;
        var aggregateRootType = typeof(SoftwareDeliveryPlanner.SharedKernel.AggregateRoot);

        // These domain entities are defined as aggregate roots by design
        var expectedAggregateRoots = new[] { "TaskItem", "TeamMember", "Holiday" };

        var domainModels = domainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace != null
                && t.Namespace.Contains("Models"))
            .ToDictionary(t => t.Name);

        var violations = new List<string>();
        foreach (var name in expectedAggregateRoots)
        {
            if (!domainModels.TryGetValue(name, out var type))
            {
                violations.Add($"{name} not found in Domain.Models");
                continue;
            }
            if (!aggregateRootType.IsAssignableFrom(type))
            {
                violations.Add($"{name} does not inherit AggregateRoot");
            }
        }

        Assert.True(violations.Count == 0,
            $"Aggregate roots must inherit SharedKernel.AggregateRoot. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void Non_Aggregate_Entities_Must_Not_Inherit_AggregateRoot()
    {
        var domainAssembly = typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly;
        var aggregateRootType = typeof(SoftwareDeliveryPlanner.SharedKernel.AggregateRoot);

        // These entities are NOT aggregate roots — they are value/child/infrastructure entities
        var nonAggregateNames = new HashSet<string> { "CalendarDay", "Allocation", "Setting", "LookupValue", "Adjustment", "Role" };

        var violations = domainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace != null
                && t.Namespace.Contains("Models"))
            .Where(t => nonAggregateNames.Contains(t.Name))
            .Where(t => aggregateRootType.IsAssignableFrom(t))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(violations.Count == 0,
            $"Non-aggregate entities must NOT inherit AggregateRoot. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ── Private Setter Rules ─────────────────────────────────

    [Theory]
    [InlineData("TaskItem")]
    [InlineData("TeamMember")]
    [InlineData("Holiday")]
    [InlineData("Adjustment")]
    public void Domain_Entities_Must_Not_Have_Public_Setters(string entityName)
    {
        var domainAssembly = typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly;

        var entityType = domainAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == entityName && t.Namespace != null
                && t.Namespace.Contains("Models"));

        Assert.NotNull(entityType);

        // Navigation/collection properties managed by EF are excluded
        var publicSetters = entityType!.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod != null && p.SetMethod.IsPublic)
            .Where(p => p.DeclaringType == entityType) // only declared, not inherited
            .Select(p => $"{entityName}.{p.Name}")
            .OrderBy(n => n)
            .ToList();

        Assert.True(publicSetters.Count == 0,
            $"Aggregate roots and child entities must use private setters for encapsulation. " +
            $"Public setters found:{Environment.NewLine}{string.Join(Environment.NewLine, publicSetters)}");
    }

    [Theory]
    [InlineData("CalendarDay")]
    [InlineData("Allocation")]
    [InlineData("Setting")]
    [InlineData("LookupValue")]
    [InlineData("Role")]
    public void Infrastructure_Entities_May_Have_Public_Setters(string entityName)
    {
        // Negative test: these non-aggregate entities are allowed to have public setters
        var domainAssembly = typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly;

        var entityType = domainAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == entityName && t.Namespace != null
                && t.Namespace.Contains("Models"));

        Assert.NotNull(entityType);

        var publicSetters = entityType!.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod != null && p.SetMethod.IsPublic)
            .Where(p => p.DeclaringType == entityType)
            .ToList();

        // These entities should have public setters (infrastructure/scheduler-managed)
        Assert.True(publicSetters.Count > 0,
            $"{entityName} is a non-aggregate entity and should have public setters. " +
            $"If it now has private setters, it may need to be reclassified as an aggregate root.");
    }

    // ── Handler Result Pattern Rules ─────────────────────────

    [Fact]
    public void Handlers_Must_Return_Result_Type()
    {
        var assembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;
        var resultType = typeof(SoftwareDeliveryPlanner.SharedKernel.Result);
        var resultGenericType = typeof(SoftwareDeliveryPlanner.SharedKernel.Result<>);

        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequestHandler<,>)))
            .ToList();

        Assert.NotEmpty(handlerTypes);

        var violations = new List<string>();
        foreach (var handler in handlerTypes)
        {
            var iface = handler.GetInterfaces().First(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequestHandler<,>));
            var returnType = iface.GetGenericArguments()[1];

            // Must be Result or Result<T>
            var isResult = returnType == resultType;
            var isResultT = returnType.IsGenericType
                && returnType.GetGenericTypeDefinition() == resultGenericType;

            if (!isResult && !isResultT)
            {
                violations.Add($"{handler.Name} returns {returnType.Name} instead of Result/Result<T>");
            }
        }

        Assert.True(violations.Count == 0,
            $"All handlers must return Result or Result<T>. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ── Domain Event Rules ───────────────────────────────────

    [Fact]
    public void Domain_Events_Must_Be_Sealed_Records()
    {
        var domainAssembly = typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly;
        var domainEventType = typeof(SoftwareDeliveryPlanner.SharedKernel.DomainEvent);

        var eventTypes = domainAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Event") && !t.IsAbstract)
            .Where(t => domainEventType.IsAssignableFrom(t))
            .ToList();

        Assert.NotEmpty(eventTypes);

        var violations = new List<string>();
        foreach (var type in eventTypes)
        {
            if (!type.IsSealed)
                violations.Add($"{type.Name} is not sealed");
            if (type.GetMethod("<Clone>$") == null)
                violations.Add($"{type.Name} is not a record type");
        }

        Assert.True(violations.Count == 0,
            $"All domain events must be 'sealed record' types. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void Domain_Events_Must_Inherit_DomainEvent()
    {
        var domainAssembly = typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly;
        var domainEventType = typeof(SoftwareDeliveryPlanner.SharedKernel.DomainEvent);
        var iDomainEventType = typeof(SoftwareDeliveryPlanner.SharedKernel.IDomainEvent);

        // Find all types in the Events namespace
        var eventTypes = domainAssembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.Contains("Events"))
            .Where(t => !t.IsAbstract && t.IsClass)
            .ToList();

        Assert.NotEmpty(eventTypes);

        var violations = eventTypes
            .Where(t => !domainEventType.IsAssignableFrom(t))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(violations.Count == 0,
            $"All types in Domain.Events namespace must inherit SharedKernel.DomainEvent. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void Domain_Events_Must_Have_OccurredOn_Property()
    {
        var domainAssembly = typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly;
        var domainEventType = typeof(SoftwareDeliveryPlanner.SharedKernel.DomainEvent);

        var eventTypes = domainAssembly.GetTypes()
            .Where(t => domainEventType.IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        Assert.NotEmpty(eventTypes);

        var violations = eventTypes
            .Where(t => t.GetProperty("OccurredOn") == null)
            .Select(t => t.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"All domain events must have an OccurredOn property (inherited from DomainEvent). " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void Aggregate_Roots_Must_Expose_DomainEvents_Collection()
    {
        var domainAssembly = typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly;
        var aggregateRootType = typeof(SoftwareDeliveryPlanner.SharedKernel.AggregateRoot);

        var aggregateRoots = domainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && aggregateRootType.IsAssignableFrom(t))
            .ToList();

        Assert.NotEmpty(aggregateRoots);

        var violations = aggregateRoots
            .Where(t => t.GetProperty("DomainEvents") == null)
            .Select(t => t.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"All aggregate roots must expose a DomainEvents collection (inherited from AggregateRoot). " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void Aggregate_Roots_Must_Have_Static_Create_Factory_Method()
    {
        var domainAssembly = typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly;
        var aggregateRootType = typeof(SoftwareDeliveryPlanner.SharedKernel.AggregateRoot);

        var aggregateRoots = domainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && aggregateRootType.IsAssignableFrom(t))
            .ToList();

        Assert.NotEmpty(aggregateRoots);

        var violations = aggregateRoots
            .Where(t => !t.GetMethods(BindingFlags.Public | BindingFlags.Static).Any(m => m.Name == "Create"))
            .Select(t => t.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"All aggregate roots must have a static Create() factory method. " +
            $"Violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // ─────────────────────────────────────────────────────────
    // Phase 2C: God Class Decomposition Architecture Rules
    // ─────────────────────────────────────────────────────────

    // ── God Class Elimination ────────────────────────────────

    [Fact]
    public void Composite_ISchedulingOrchestrator_Must_Not_Exist()
    {
        var appAssembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        var composite = appAssembly.GetTypes()
            .FirstOrDefault(t => t.IsInterface && t.Name == "ISchedulingOrchestrator");

        Assert.Null(composite);
    }

    [Fact]
    public void Composite_SchedulingOrchestrator_Must_Not_Exist()
    {
        var infraAssembly = typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly;

        var composite = infraAssembly.GetTypes()
            .FirstOrDefault(t => t.IsClass && t.Name == "SchedulingOrchestrator");

        Assert.Null(composite);
    }

    // ── Focused Service Rules ────────────────────────────────

    [Theory]
    [InlineData("TaskService")]
    [InlineData("ResourceService")]
    [InlineData("AdjustmentService")]
    [InlineData("HolidayService")]
    [InlineData("SchedulerService")]
    [InlineData("PlanningQueryService")]
    public void Focused_Services_Must_Be_Internal_And_Sealed(string serviceName)
    {
        var infraAssembly = typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly;

        var serviceType = infraAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == serviceName && t.IsClass);

        Assert.NotNull(serviceType);
        Assert.False(serviceType!.IsPublic,
            $"{serviceName} must be internal (not public).");
        Assert.True(serviceType.IsSealed,
            $"{serviceName} must be sealed.");
    }

    [Theory]
    [InlineData("TaskService")]
    [InlineData("ResourceService")]
    [InlineData("AdjustmentService")]
    [InlineData("HolidayService")]
    [InlineData("SchedulerService")]
    [InlineData("PlanningQueryService")]
    public void Focused_Services_Must_Inherit_ServiceBase(string serviceName)
    {
        var infraAssembly = typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly;

        var serviceType = infraAssembly.GetTypes()
            .First(t => t.Name == serviceName && t.IsClass);

        var serviceBaseType = infraAssembly.GetTypes()
            .First(t => t.Name == "ServiceBase" && t.IsClass && t.IsAbstract);

        Assert.True(serviceBaseType.IsAssignableFrom(serviceType),
            $"{serviceName} must inherit from ServiceBase.");
    }

    [Fact]
    public void ServiceBase_Must_Be_Internal_Abstract()
    {
        var infraAssembly = typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly;

        var serviceBaseType = infraAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "ServiceBase" && t.IsClass);

        Assert.NotNull(serviceBaseType);
        Assert.False(serviceBaseType!.IsPublic,
            "ServiceBase must be internal (not public).");
        Assert.True(serviceBaseType.IsAbstract,
            "ServiceBase must be abstract.");
    }

    [Theory]
    [InlineData("TaskService", "ITaskOrchestrator")]
    [InlineData("ResourceService", "IResourceOrchestrator")]
    [InlineData("AdjustmentService", "IAdjustmentOrchestrator")]
    [InlineData("HolidayService", "IHolidayOrchestrator")]
    [InlineData("SchedulerService", "ISchedulerService")]
    [InlineData("PlanningQueryService", "IPlanningQueryService")]
    public void Focused_Services_Must_Implement_Correct_Application_Interface(
        string serviceName, string interfaceName)
    {
        var infraAssembly = typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly;
        var appAssembly   = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        var serviceType = infraAssembly.GetTypes()
            .First(t => t.Name == serviceName && t.IsClass);

        var expectedInterface = appAssembly.GetTypes()
            .First(t => t.IsInterface && t.Name == interfaceName);

        Assert.True(expectedInterface.IsAssignableFrom(serviceType),
            $"{serviceName} must implement {interfaceName}.");
    }

    [Theory]
    [InlineData("TaskService")]
    [InlineData("ResourceService")]
    [InlineData("AdjustmentService")]
    [InlineData("HolidayService")]
    [InlineData("SchedulerService")]
    [InlineData("PlanningQueryService")]
    public void Focused_Services_Must_Implement_Exactly_One_Application_Interface(string serviceName)
    {
        var infraAssembly = typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly;
        var appAssembly   = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        var appInterfaces = appAssembly.GetTypes()
            .Where(t => t.IsInterface)
            .ToHashSet();

        var serviceType = infraAssembly.GetTypes()
            .First(t => t.Name == serviceName && t.IsClass);

        var implementedAppInterfaces = serviceType.GetInterfaces()
            .Where(i => appInterfaces.Contains(i))
            .ToList();

        Assert.True(implementedAppInterfaces.Count == 1,
            $"{serviceName} must implement exactly one Application interface. " +
            $"Found {implementedAppInterfaces.Count}: {string.Join(", ", implementedAppInterfaces.Select(i => i.Name))}");
    }

    // ── SchedulingEngine Abstraction Rules ───────────────────

    [Fact]
    public void SchedulingEngineFactory_Must_Be_Internal_Sealed()
    {
        var infraAssembly = typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly;

        var factoryType = infraAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "SchedulingEngineFactory" && t.IsClass);

        Assert.NotNull(factoryType);
        Assert.False(factoryType!.IsPublic,
            "SchedulingEngineFactory must be internal (not public).");
        Assert.True(factoryType.IsSealed,
            "SchedulingEngineFactory must be sealed.");
    }

    [Fact]
    public void ISchedulingEngine_Must_Extend_IDisposable()
    {
        var appAssembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;

        var engineInterface = appAssembly.GetTypes()
            .FirstOrDefault(t => t.IsInterface && t.Name == "ISchedulingEngine");

        Assert.NotNull(engineInterface);
        Assert.True(typeof(IDisposable).IsAssignableFrom(engineInterface),
            "ISchedulingEngine must extend IDisposable to ensure callers dispose engine instances.");
    }

    // ── DTO Enum Allow-List Guard ────────────────────────────

    [Fact]
    public void AllowedDtoEnums_Must_Reference_Existing_Types()
    {
        // Guard against stale entries in the allow-list. If an allowed enum
        // is renamed or deleted, this test will catch it.
        var appAssembly = typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly;
        var allTypes = appAssembly.GetTypes()
            .Where(t => t.IsEnum)
            .Select(t => t.FullName ?? t.Name)
            .ToHashSet(StringComparer.Ordinal);

        var stale = AllowedDtoEnums
            .Where(name => !allTypes.Contains(name))
            .OrderBy(n => n)
            .ToList();

        Assert.True(stale.Count == 0,
            $"AllowedDtoEnums contains stale entries that no longer exist in the Application assembly. " +
            $"Remove:{Environment.NewLine}{string.Join(Environment.NewLine, stale)}");
    }

    // ─────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Checks whether <paramref name="type"/> is assignable to a base type
    /// identified by its full name, without requiring a direct assembly reference.
    /// Walks the full inheritance chain.
    /// </summary>
    private static bool IsAssignableToName(Type type, string fullName)
    {
        var current = type;
        while (current != null)
        {
            if (current.FullName == fullName) return true;
            current = current.BaseType;
        }
        return false;
    }
}
