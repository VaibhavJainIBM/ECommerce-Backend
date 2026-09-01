using ECommerce.Application.Common;

namespace ECommerce.Application.Authentication;

public static class AuthenticationErrors
{
    public static readonly Error FirstNameRequired = new(
        "auth.first_name_required",
        "First name is required.");

    public static readonly Error LastNameRequired = new(
        "auth.last_name_required",
        "Last name is required.");

    public static readonly Error NameTooLong = new(
        "auth.name_too_long",
        "First name and last name cannot exceed 100 characters.");

    public static readonly Error EmailRequired = new(
        "auth.email_required",
        "Email is required.");

    public static readonly Error InvalidEmail = new(
        "auth.invalid_email",
        "Email format is invalid.");

    public static readonly Error PasswordRequired = new(
        "auth.password_required",
        "Password is required.");

    public static readonly Error DuplicateEmail = new(
        "auth.duplicate_email",
        "An account with this email already exists.");

    public static readonly Error InvalidCredentials = new(
        "auth.invalid_credentials",
        "Email or password is incorrect.");

    public static Error IdentityValidation(string description)
    {
        return new Error(
            "auth.identity_validation",
            description);
    }
}