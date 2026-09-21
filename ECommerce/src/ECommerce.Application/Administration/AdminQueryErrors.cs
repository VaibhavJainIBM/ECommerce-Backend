using ECommerce.Application.Common;

namespace ECommerce.Application.Administration;

public static class AdminQueryErrors
{
    public static readonly Error PageInvalid = new(
        "AdminQuery.PageInvalid",
        "Page must be at least 1.");

    public static readonly Error PageSizeInvalid = new(
        "AdminQuery.PageSizeInvalid",
        "Page size must be between 1 and 100.");

    public static readonly Error SearchTooLong = new(
        "AdminQuery.SearchTooLong",
        "Search cannot exceed 100 characters.");

    public static Error InvalidStatus(string status) => new(
        "AdminQuery.InvalidStatus",
        $"'{status}' is not a valid status for this queue.");

    public static readonly Error PaginationTooDeep = new(
        "AdminQuery.PaginationTooDeep",
        "The requested page is too large.");
}
