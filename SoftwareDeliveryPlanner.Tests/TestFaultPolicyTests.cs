using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Web.Services.Testing;

namespace SoftwareDeliveryPlanner.Tests;

/// <summary>
/// Unit tests for the test-fault seam: <see cref="NoOpTestFaultPolicy"/> (production
/// default) and <see cref="InMemoryTestFaultPolicy"/> (e2e-only, env-gated).
/// </summary>
public class TestFaultPolicyTests
{
    [Fact]
    public void NoOpPolicy_NeverThrows()
    {
        var policy = new NoOpTestFaultPolicy();
        var ex = Record.Exception(() => policy.MaybeThrow("AnyKey"));
        Assert.Null(ex);
    }

    [Fact]
    public void NoOpPolicy_AcceptsNullOrEmptyOperation()
    {
        var policy = new NoOpTestFaultPolicy();
        var e1 = Record.Exception(() => policy.MaybeThrow(""));
        var e2 = Record.Exception(() => policy.MaybeThrow(null!));
        Assert.Null(e1);
        Assert.Null(e2);
    }

    [Fact]
    public void InMemoryPolicy_Disabled_NeverThrows()
    {
        var policy = new InMemoryTestFaultPolicy(enabled: false);
        policy.Arm("Op"); // arm calls are silently ignored when disabled
        var ex = Record.Exception(() => policy.MaybeThrow("Op"));
        Assert.Null(ex);
    }

    [Fact]
    public void InMemoryPolicy_Enabled_NotArmed_DoesNotThrow()
    {
        var policy = new InMemoryTestFaultPolicy(enabled: true);
        var ex = Record.Exception(() => policy.MaybeThrow("Op"));
        Assert.Null(ex);
    }

    [Fact]
    public void InMemoryPolicy_Enabled_Armed_ThrowsTestInjectedFaultException()
    {
        var policy = new InMemoryTestFaultPolicy(enabled: true);
        policy.Arm("GanttSegments");
        var ex = Assert.Throws<TestInjectedFaultException>(() => policy.MaybeThrow("GanttSegments"));
        Assert.Equal("GanttSegments", ex.OperationKey);
    }

    [Fact]
    public void InMemoryPolicy_OperationKey_IsCaseInsensitive()
    {
        var policy = new InMemoryTestFaultPolicy(enabled: true);
        policy.Arm("GanttSegments");
        Assert.Throws<TestInjectedFaultException>(() => policy.MaybeThrow("ganttsegments"));
        Assert.Throws<TestInjectedFaultException>(() => policy.MaybeThrow("GANTTSEGMENTS"));
    }

    [Fact]
    public void InMemoryPolicy_Clear_RemovesArmedKey()
    {
        var policy = new InMemoryTestFaultPolicy(enabled: true);
        policy.Arm("Op1");
        policy.Arm("Op2");
        policy.Clear("Op1");
        var ex = Record.Exception(() => policy.MaybeThrow("Op1"));
        Assert.Null(ex);
        // Op2 still armed
        Assert.Throws<TestInjectedFaultException>(() => policy.MaybeThrow("Op2"));
    }

    [Fact]
    public void InMemoryPolicy_ClearAll_RemovesEverything()
    {
        var policy = new InMemoryTestFaultPolicy(enabled: true);
        policy.Arm("A");
        policy.Arm("B");
        policy.Arm("C");
        policy.ClearAll();
        Assert.Null(Record.Exception(() => policy.MaybeThrow("A")));
        Assert.Null(Record.Exception(() => policy.MaybeThrow("B")));
        Assert.Null(Record.Exception(() => policy.MaybeThrow("C")));
    }

    [Fact]
    public void InMemoryPolicy_OnlyArmedKey_Throws_OthersDoNot()
    {
        var policy = new InMemoryTestFaultPolicy(enabled: true);
        policy.Arm("GanttSegments");
        Assert.Throws<TestInjectedFaultException>(() => policy.MaybeThrow("GanttSegments"));
        Assert.Null(Record.Exception(() => policy.MaybeThrow("OtherOp")));
    }

    [Fact]
    public void InMemoryPolicy_NullOrEmptyOperationKey_DoesNotThrow()
    {
        var policy = new InMemoryTestFaultPolicy(enabled: true);
        Assert.Null(Record.Exception(() => policy.MaybeThrow("")));
        Assert.Null(Record.Exception(() => policy.MaybeThrow(null!)));
    }

    [Fact]
    public void TestInjectedFaultException_MessageMentionsOperation()
    {
        var ex = new TestInjectedFaultException("MyOp");
        Assert.Contains("MyOp", ex.Message);
        Assert.Equal("MyOp", ex.OperationKey);
    }
}
