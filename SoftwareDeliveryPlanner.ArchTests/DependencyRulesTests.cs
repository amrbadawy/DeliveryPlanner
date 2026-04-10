using NetArchTest.Rules;
using System.Collections;

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
    public void Domain_Does_Not_Depend_On_Blazor()
    {
        var result = Types
            .InAssembly(typeof(SoftwareDeliveryPlanner.Domain.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("SoftwareDeliveryPlanner.Blazor")
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
    public void Application_Does_Not_Depend_On_Blazor()
    {
        var result = Types
            .InAssembly(typeof(SoftwareDeliveryPlanner.Application.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("SoftwareDeliveryPlanner.Blazor")
            .GetResult();

        Assert.True(result.IsSuccessful, BuildFailureMessage(result.FailingTypeNames));
    }

    [Fact]
    public void Infrastructure_Does_Not_Depend_On_Blazor()
    {
        var result = Types
            .InAssembly(typeof(SoftwareDeliveryPlanner.Infrastructure.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("SoftwareDeliveryPlanner.Blazor")
            .GetResult();

        Assert.True(result.IsSuccessful, BuildFailureMessage(result.FailingTypeNames));
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
}
