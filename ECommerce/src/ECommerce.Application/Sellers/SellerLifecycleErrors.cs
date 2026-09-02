using ECommerce.Application.Common;

namespace ECommerce.Application.Sellers;

public static class SellerLifecycleErrors
{
    public const string SellerNotFoundCode =
        "seller_lifecycle.seller_not_found";

    public const string StateConflictCode =
        "seller_lifecycle.state_conflict";

    public static readonly Error SellerIdRequired = new(
        "seller_lifecycle.seller_id_required",
        "Seller ID is required.");

    public static Error SellerNotFound(Guid sellerId)
    {
        return new Error(
            SellerNotFoundCode,
            $"Seller '{sellerId}' was not found.");
    }

    public static Error CannotSubmitForReview(string status)
    {
        return new Error(
            StateConflictCode,
            $"A seller with status '{status}' cannot be " +
            "submitted for review.");
    }

    public static Error CannotApprove(string status)
    {
        return new Error(
            StateConflictCode,
            $"A seller with status '{status}' cannot be approved.");
    }
}