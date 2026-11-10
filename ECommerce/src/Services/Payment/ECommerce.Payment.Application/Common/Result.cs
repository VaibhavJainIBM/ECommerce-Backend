namespace ECommerce.Payment.Application.Common;

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
        => new(true, value, []);

    public static Result<T> Failure(Error error)
        => new(false, default, [error]);
}

public static class PaymentErrors
{
    public static readonly Error Unauthenticated =
        new(
            "Payment.Unauthenticated",
            "Authentication is required.");

    public static Error Validation(string message) =>
        new(
            "Payment.Validation",
            message);

    public static Error NotFound(string message) =>
        new(
            "Payment.NotFound",
            message);

    public static Error Conflict(string message) =>
        new(
            "Payment.Conflict",
            message);

    public static Error Dependency(string message) =>
        new(
            "Payment.Dependency",
            message);
}