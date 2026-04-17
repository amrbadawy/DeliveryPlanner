namespace SoftwareDeliveryPlanner.SharedKernel;

/// <summary>
/// Represents the outcome of an operation that does not return a value.
/// Use <see cref="Result{T}"/> when a value is expected on success.
/// </summary>
public readonly struct Result : IEquatable<Result>
{
    /// <summary>The error associated with a failed result.</summary>
    public Error Error { get; }

    /// <summary>True when the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>True when the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>Creates a failed result with the specified error.</summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>Implicitly converts an <see cref="Error"/> to a failed <see cref="Result"/>.</summary>
    public static implicit operator Result(Error error) => Failure(error);

    public bool Equals(Result other) => IsSuccess == other.IsSuccess && Error == other.Error;
    public override bool Equals(object? obj) => obj is Result other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(IsSuccess, Error);
    public static bool operator ==(Result left, Result right) => left.Equals(right);
    public static bool operator !=(Result left, Result right) => !left.Equals(right);

    public override string ToString() => IsSuccess ? "Success" : $"Failure({Error.Code}: {Error.Message})";
}
