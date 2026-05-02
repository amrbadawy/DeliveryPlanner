namespace SoftwareDeliveryPlanner.Application.Abstractions;

/// <summary>
/// Test-only seam for injecting deterministic faults into specific application
/// operations. The production composition uses <see cref="NoOpTestFaultPolicy"/>,
/// which never throws. End-to-end tests can register an alternative implementation
/// — gated by the <c>SDP_TEST_FAULTS=1</c> environment variable — to exercise
/// failure-handling UI paths (loading skeleton → error state, retry, etc.) that
/// would otherwise be unreachable in deterministic happy-path runs.
/// </summary>
/// <remarks>
/// The contract is intentionally minimal: callers pass an opaque
/// <paramref name="operationKey"/> identifying the call site (e.g.
/// <c>"GanttSegments"</c>) and the policy decides whether to throw.
/// Implementations MUST be thread-safe.
/// </remarks>
public interface ITestFaultPolicy
{
    /// <summary>
    /// Throws an implementation-defined exception if a fault is currently armed
    /// for <paramref name="operationKey"/>; otherwise returns immediately.
    /// In production this is always a no-op.
    /// </summary>
    void MaybeThrow(string operationKey);
}

/// <summary>
/// Production default — never throws. Registered unconditionally so the
/// fault-policy seam is always present in DI but inert outside tests.
/// </summary>
public sealed class NoOpTestFaultPolicy : ITestFaultPolicy
{
    public void MaybeThrow(string operationKey) { /* no-op */ }
}
