using System.Collections.Concurrent;
using SoftwareDeliveryPlanner.Application.Abstractions;

namespace SoftwareDeliveryPlanner.Web.Services.Testing;

/// <summary>
/// Test-only fault policy gated by the <c>SDP_TEST_FAULTS=1</c> environment variable.
/// Maintains an in-memory set of "armed" operation keys; <see cref="MaybeThrow"/>
/// throws <see cref="TestInjectedFaultException"/> when called with an armed key.
/// Always returns no-op when the env var is not set.
/// </summary>
/// <remarks>
/// Registered in <c>Program.cs</c> in place of <see cref="NoOpTestFaultPolicy"/>
/// only when <c>SDP_TEST_FAULTS=1</c>. The minimal-API endpoints
/// <c>POST /test-faults/arm</c> and <c>POST /test-faults/clear</c> mutate the
/// armed-set; they are also gated on the same env var.
/// </remarks>
public sealed class InMemoryTestFaultPolicy : ITestFaultPolicy
{
    private readonly ConcurrentDictionary<string, byte> _armed = new(StringComparer.OrdinalIgnoreCase);
    private readonly bool _enabled;

    public InMemoryTestFaultPolicy(bool enabled) => _enabled = enabled;

    public void MaybeThrow(string operationKey)
    {
        if (!_enabled) return;
        if (string.IsNullOrWhiteSpace(operationKey)) return;
        if (_armed.ContainsKey(operationKey))
            throw new TestInjectedFaultException(operationKey);
    }

    public void Arm(string operationKey)
    {
        if (!_enabled) return;
        if (!string.IsNullOrWhiteSpace(operationKey))
            _armed[operationKey] = 1;
    }

    public void Clear(string operationKey)
    {
        if (!_enabled) return;
        if (!string.IsNullOrWhiteSpace(operationKey))
            _armed.TryRemove(operationKey, out _);
    }

    public void ClearAll() => _armed.Clear();
}

/// <summary>
/// Sentinel exception type emitted by <see cref="InMemoryTestFaultPolicy"/>.
/// Distinct type so production handlers can choose to surface a generic
/// "operation failed" message rather than leaking the test marker.
/// </summary>
public sealed class TestInjectedFaultException : Exception
{
    public string OperationKey { get; }

    public TestInjectedFaultException(string operationKey)
        : base($"Test-injected fault for operation '{operationKey}'.")
        => OperationKey = operationKey;
}
