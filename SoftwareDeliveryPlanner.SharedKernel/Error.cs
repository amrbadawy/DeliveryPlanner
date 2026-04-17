namespace SoftwareDeliveryPlanner.SharedKernel;

/// <summary>
/// Represents an error with a code and human-readable message.
/// Used as the failure payload in <see cref="Result"/> and <see cref="Result{T}"/>.
/// </summary>
public sealed record Error(string Code, string Message)
{
    /// <summary>Sentinel value representing no error.</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>Creates a validation error.</summary>
    public static Error Validation(string message) => new("Validation", message);

    /// <summary>Creates a not-found error.</summary>
    public static Error NotFound(string message) => new("NotFound", message);

    /// <summary>Creates a conflict error (e.g. duplicate, overlap).</summary>
    public static Error Conflict(string message) => new("Conflict", message);

    /// <summary>Creates an error with a custom code.</summary>
    public static Error Custom(string code, string message) => new(code, message);
}
