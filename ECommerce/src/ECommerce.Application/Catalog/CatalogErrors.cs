using ECommerce.Application.Common;

namespace ECommerce.Application.Catalog;

public static class CatalogErrors
{
    public const string GtinConflictCode =
        "catalog.gtin_conflict";

    public static readonly Error RequestRequired = new(
        "catalog.request_required",
        "Product details are required.");

    public static readonly Error TitleRequired = new(
        "catalog.title_required",
        "Product title is required.");

    public static readonly Error TitleTooLong = new(
        "catalog.title_too_long",
        "Product title cannot exceed 250 characters.");

    public static readonly Error BrandNameRequired = new(
        "catalog.brand_name_required",
        "Brand name is required.");

    public static readonly Error BrandNameTooLong = new(
        "catalog.brand_name_too_long",
        "Brand name cannot exceed 150 characters.");

    public static readonly Error DescriptionTooLong = new(
        "catalog.description_too_long",
        "Product description cannot exceed 4000 characters.");

    public static readonly Error VariantsRequired = new(
        "catalog.variants_required",
        "At least one product variant is required.");

    public static readonly Error TooManyVariants = new(
        "catalog.too_many_variants",
        "A product cannot contain more than 100 variants.");

    public static Error VariantRequired(int index)
    {
        return new Error(
            "catalog.variant_required",
            $"Variant {index + 1} is required.");
    }

    public static Error VariantNameRequired(int index)
    {
        return new Error(
            "catalog.variant_name_required",
            $"Variant {index + 1} name is required.");
    }

    public static Error VariantNameTooLong(int index)
    {
        return new Error(
            "catalog.variant_name_too_long",
            $"Variant {index + 1} name cannot exceed " +
            "150 characters.");
    }

    public static Error VariantCodeRequired(int index)
    {
        return new Error(
            "catalog.variant_code_required",
            $"Variant {index + 1} code is required.");
    }

    public static Error VariantCodeTooLong(int index)
    {
        return new Error(
            "catalog.variant_code_too_long",
            $"Variant {index + 1} code cannot exceed " +
            "64 characters.");
    }

    public static Error InvalidVariantCode(int index)
    {
        return new Error(
            "catalog.invalid_variant_code",
            $"Variant {index + 1} code may contain only " +
            "letters, numbers, hyphens, underscores, " +
            "and periods.");
    }

    public static Error DuplicateVariantCode(
        string variantCode)
    {
        return new Error(
            "catalog.duplicate_variant_code",
            $"Variant code '{variantCode}' appears more " +
            "than once.");
    }

    public static Error InvalidGtin(int index)
    {
        return new Error(
            "catalog.invalid_gtin",
            $"Variant {index + 1} GTIN must contain " +
            "8, 12, 13, or 14 digits.");
    }

    public static Error DuplicateGtinInRequest(
        string gtin)
    {
        return new Error(
            "catalog.duplicate_gtin_in_request",
            $"GTIN '{gtin}' appears more than once.");
    }

    public static Error GtinAlreadyExists(string gtin)
    {
        return new Error(
            GtinConflictCode,
            $"GTIN '{gtin}' already belongs to another " +
            "product variant.");
    }

    public static readonly Error ConcurrentGtinConflict = new(
        GtinConflictCode,
        "A supplied GTIN was assigned by another request. " +
        "Refresh and try again.");

    public const string ProductNotFoundCode =
    "catalog.product_not_found";

    public const string ActivationConflictCode =
        "catalog.activation_conflict";

    public static Error ProductNotFound(Guid productId)
    {
        return new Error(
            ProductNotFoundCode,
            $"Product '{productId}' was not found.");
    }

    public static readonly Error ProductHasNoVariants = new(
        ActivationConflictCode,
        "A product must contain at least one variant " +
        "before activation.");

    public static Error ProductCannotBeActivated(
        string currentStatus)
    {
        return new Error(
            ActivationConflictCode,
            $"A product with status '{currentStatus}' " +
            "cannot be activated.");
    }

    public static Error VariantCannotBeActivated(
        string variantCode,
        string currentStatus)
    {
        return new Error(
            ActivationConflictCode,
            $"Variant '{variantCode}' with status " +
            $"'{currentStatus}' cannot be activated.");
    }


}