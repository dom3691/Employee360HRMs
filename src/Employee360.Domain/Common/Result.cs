namespace Employee360.Domain.Common;

/// <summary>
/// Represents the outcome of an operation without a return value.
/// Business-rule failures are expressed as failed results (never exceptions),
/// per the project handler conventions.
/// </summary>
public class Result
{
    /// <summary>True when the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>True when the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Error messages describing why the operation failed; empty on success.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>First error message, or null when successful.</summary>
    public string? Error => Errors.Count > 0 ? Errors[0] : null;

    protected Result(bool isSuccess, IReadOnlyList<string> errors)
    {
        if (isSuccess && errors.Count > 0)
        {
            throw new InvalidOperationException("A successful result cannot contain errors.");
        }

        if (!isSuccess && errors.Count == 0)
        {
            throw new InvalidOperationException("A failed result must contain at least one error.");
        }

        IsSuccess = isSuccess;
        Errors = errors;
    }

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new(true, []);

    /// <summary>Creates a failed result with one or more error messages.</summary>
    /// <param name="errors">At least one error message.</param>
    public static Result Failure(params string[] errors) => new(false, errors);

    /// <summary>Creates a failed result from a collection of error messages.</summary>
    /// <param name="errors">At least one error message.</param>
    public static Result Failure(IEnumerable<string> errors) => new(false, errors.ToList());

    /// <summary>Creates a successful result carrying a value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value produced by the operation.</param>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>Creates a failed result of <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="errors">At least one error message.</param>
    public static Result<T> Failure<T>(params string[] errors) => Result<T>.Failure(errors);
}

/// <summary>
/// Represents the outcome of an operation that returns <typeparamref name="T"/> on success.
/// </summary>
/// <typeparam name="T">The type of the value produced on success.</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// The value produced by a successful operation.
    /// Accessing this on a failed result throws <see cref="InvalidOperationException"/>.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    private Result(bool isSuccess, T? value, IReadOnlyList<string> errors)
        : base(isSuccess, errors)
    {
        _value = value;
    }

    /// <summary>Creates a successful result carrying <paramref name="value"/>.</summary>
    public static Result<T> Success(T value) => new(true, value, []);

    /// <summary>Creates a failed result with one or more error messages.</summary>
    public static new Result<T> Failure(params string[] errors) => new(false, default, errors);

    /// <summary>Creates a failed result from a collection of error messages.</summary>
    public static new Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors.ToList());

    /// <summary>Implicitly wraps a value in a successful result.</summary>
    public static implicit operator Result<T>(T value) => Success(value);
}
