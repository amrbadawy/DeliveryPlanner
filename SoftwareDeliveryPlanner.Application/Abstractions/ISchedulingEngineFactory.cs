namespace SoftwareDeliveryPlanner.Application.Abstractions;

/// <summary>
/// Creates <see cref="ISchedulingEngine"/> instances, each with its own
/// database context. Callers must dispose the returned engine.
/// </summary>
public interface ISchedulingEngineFactory
{
    /// <summary>
    /// Creates a new scheduling engine backed by a fresh database context.
    /// The returned instance must be disposed to release the context.
    /// </summary>
    Task<ISchedulingEngine> CreateAsync(CancellationToken cancellationToken = default);
}
