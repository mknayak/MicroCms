namespace MicroCMS.Shared.Results;

/// <summary>
/// Represents the outcome of an operation that can either succeed or fail with a typed error.
/// Cyclomatic complexity target: ≤ 5 per method.
/// </summary>
public sealed class Result
{
    private Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);
    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Failure(error);
}

/// <summary>
/// Represents the outcome of an operation that returns a value on success.
/// </summary>
public sealed class Result<TValue>
{
    private readonly TValue? _value;

    private Result(TValue value, bool isSuccess, Error error)
    {
        _value = value;
        IsSuccess = isSuccess;
        Error = error;
    }

    private Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public TValue Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access Value on a failed result.");

    internal static Result<TValue> Success(TValue value) => new(value, true, Error.None);
    internal static Result<TValue> Failure(Error error) => new(false, error);

    /// <summary>Named method for implicit conversion (CA2225).</summary>
    public static Result<TValue> FromValue(TValue value) => Success(value);

    public static implicit operator Result<TValue>(TValue value) => FromValue(value);
}
