namespace SoftwareDeliveryPlanner.SharedKernel;

/// <summary>
/// Represents the outcome of an operation that returns a value of type <typeparamref name="T"/> on success.
/// Use <see cref="Result"/> for void operations.
/// </summary>
public readonly struct Result<T> : IEquatable<Result<T>>
{
    private readonly T _value;

    /// <summary>
    /// The value produced by a successful operation.
    /// Throws <see cref="InvalidOperationException"/> if accessed on a failed result.
    /// </summary>
    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException($"Cannot access Value on a failed result. Error: {Error}");

    /// <summary>The error associated with a failed result.</summary>
    public Error Error { get; }

    /// <summary>True when the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>True when the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    private Result(T value)
    {
        _value = value;
        IsSuccess = true;
        Error = Error.None;
    }

    private Result(Error error)
    {
        _value = default!;
        IsSuccess = false;
        Error = error;
    }

    /// <summary>Creates a successful result with the specified value.</summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>Creates a failed result with the specified error.</summary>
    public static Result<T> Failure(Error error) => new(error);

    /// <summary>Implicitly converts a value to a successful <see cref="Result{T}"/>.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Implicitly converts an <see cref="Error"/> to a failed <see cref="Result{T}"/>.</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);

    /// <summary>
    /// Maps the value to a new type if the result is successful.
    /// Returns a failed result with the same error if the result is a failure.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper) =>
        IsSuccess ? Result<TOut>.Success(mapper(_value)) : Result<TOut>.Failure(Error);

    /// <summary>
    /// Executes <paramref name="onSuccess"/> if the result is successful,
    /// or <paramref name="onFailure"/> if it failed.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(_value) : onFailure(Error);

    public bool Equals(Result<T> other) =>
        IsSuccess == other.IsSuccess && Error == other.Error &&
        EqualityComparer<T>.Default.Equals(_value, other._value);

    public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(IsSuccess, Error, _value);
    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);
    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);

    public override string ToString() => IsSuccess ? $"Success({_value})" : $"Failure({Error.Code}: {Error.Message})";
}
