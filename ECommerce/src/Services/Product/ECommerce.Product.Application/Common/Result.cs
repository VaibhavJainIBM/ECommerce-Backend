namespace ECommerce.Product.Application.Common;

public sealed record Error(
    string Code,
    string Description);

public sealed class Result<T>
{
    private Result(
        bool isSuccess,
        T? value,
        IReadOnlyCollection<Error> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T? Value { get; }

    public IReadOnlyCollection<Error> Errors { get; }

    public static Result<T> Success(T value)
    {
        return new Result<T>(
            true,
            value,
            Array.Empty<Error>());
    }

    public static Result<T> Failure(
        params Error[] errors)
    {
        return new Result<T>(
            false,
            default,
            errors);
    }

    public static Result<T> Failure(
        IEnumerable<Error> errors)
    {
        return new Result<T>(
            false,
            default,
            errors.ToArray());
    }
}