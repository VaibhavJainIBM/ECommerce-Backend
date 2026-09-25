namespace ECommerce.User.Application.Common;

public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, IReadOnlyCollection<Error> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public IReadOnlyCollection<Error> Errors { get; }

    public static Result<T> Success(T value) => new(true, value, Array.Empty<Error>());
    public static Result<T> Failure(params Error[] errors) => new(false, default, errors);
    public static Result<T> Failure(IEnumerable<Error> errors) =>
        new(false, default, errors.ToArray());
}
