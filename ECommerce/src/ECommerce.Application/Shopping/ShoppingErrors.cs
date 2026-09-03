using ECommerce.Application.Common;

namespace ECommerce.Application.Shopping;

public static class ShoppingErrors
{
    public const string UnauthenticatedCode = "shopping.unauthenticated";
    public const string AccountUnavailableCode = "shopping.account_unavailable";
    public const string NotFoundCode = "shopping.not_found";
    public const string ConflictCode = "shopping.conflict";
    public const string ValidationCode = "shopping.validation";

    public static readonly Error Unauthenticated = new(UnauthenticatedCode, "Sign in to continue.");
    public static readonly Error AccountUnavailable = new(AccountUnavailableCode, "This customer account is unavailable.");
    public static Error NotFound(string message) => new(NotFoundCode, message);
    public static Error Conflict(string message) => new(ConflictCode, message);
    public static Error Validation(string message) => new(ValidationCode, message);
}
