using ECommerce.Application.Common;

namespace ECommerce.Application.Sellers;

public static class SellerErrors
{
    public static readonly Error RequestRequired = new(
        "seller.request_required",
        "Seller details are required.");

    public static readonly Error DisplayNameRequired = new(
        "seller.display_name_required",
        "Display name is required.");

    public static readonly Error DisplayNameTooLong = new(
        "seller.display_name_too_long",
        "Display name cannot exceed 150 characters.");

    public static readonly Error LegalBusinessNameRequired = new(
        "seller.legal_business_name_required",
        "Legal business name is required.");

    public static readonly Error LegalBusinessNameTooLong = new(
        "seller.legal_business_name_too_long",
        "Legal business name cannot exceed 250 characters.");

    public static readonly Error CurrentUserUnavailable = new(
        "seller.current_user_unavailable",
        "The authenticated user could not be identified.");
}