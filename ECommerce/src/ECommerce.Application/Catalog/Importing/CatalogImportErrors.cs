using ECommerce.Application.Common;

namespace ECommerce.Application.Catalog.Importing;

public static class CatalogImportErrors
{
    public const string GtinConflictCode =
        "catalog.gtin_conflict";

    public const string FileTooLargeCode =
        "catalog_import.file_too_large";

    public static readonly Error FileRequired = new(
        "catalog_import.file_required",
        "Choose a CSV file to import.");

    public static readonly Error FileEmpty = new(
        "catalog_import.file_empty",
        "The CSV file is empty.");

    public static readonly Error InvalidFileExtension = new(
        "catalog_import.invalid_file_extension",
        "The uploaded file must have a .csv extension.");

    public static readonly Error FileTooLarge = new(
        FileTooLargeCode,
        "The CSV file cannot exceed 2 MB.");

    public static readonly Error HeaderRequired = new(
        "catalog_import.header_required",
        "The CSV header row is required.");

    public static Error MissingHeader(string header)
    {
        return new Error(
            "catalog_import.missing_header",
            $"Required CSV header '{header}' is missing.");
    }

    public static Error UnexpectedHeader(string header)
    {
        return new Error(
            "catalog_import.unexpected_header",
            $"CSV header '{header}' is not supported.");
    }

    public static Error DuplicateHeader(string header)
    {
        return new Error(
            "catalog_import.duplicate_header",
            $"CSV header '{header}' appears more than once.");
    }

    public static readonly Error InvalidCsv = new(
        "catalog_import.invalid_csv",
        "The file is not a valid UTF-8 CSV document.");

    public static readonly Error NoRows = new(
        "catalog_import.no_rows",
        "The CSV file must contain at least one data row.");

    public static readonly Error TooManyRows = new(
        "catalog_import.too_many_rows",
        "A CSV import cannot contain more than 1000 data rows.");

    public static Error ProductKeyRequired(int row) =>
        RowError(
            "catalog_import.product_key_required",
            row,
            "ProductKey is required.");

    public static Error ProductKeyTooLong(int row) =>
        RowError(
            "catalog_import.product_key_too_long",
            row,
            "ProductKey cannot exceed 64 characters.");

    public static Error InvalidProductKey(int row) =>
        RowError(
            "catalog_import.invalid_product_key",
            row,
            "ProductKey may contain only letters, numbers, hyphens, underscores, and periods.");

    public static Error TitleRequired(int row) =>
        RowError(
            "catalog_import.title_required",
            row,
            "Title is required.");

    public static Error TitleTooLong(int row) =>
        RowError(
            "catalog_import.title_too_long",
            row,
            "Title cannot exceed 250 characters.");

    public static Error BrandNameRequired(int row) =>
        RowError(
            "catalog_import.brand_name_required",
            row,
            "BrandName is required.");

    public static Error BrandNameTooLong(int row) =>
        RowError(
            "catalog_import.brand_name_too_long",
            row,
            "BrandName cannot exceed 150 characters.");

    public static Error DescriptionTooLong(int row) =>
        RowError(
            "catalog_import.description_too_long",
            row,
            "Description cannot exceed 4000 characters.");

    public static Error VariantNameRequired(int row) =>
        RowError(
            "catalog_import.variant_name_required",
            row,
            "VariantName is required.");

    public static Error VariantNameTooLong(int row) =>
        RowError(
            "catalog_import.variant_name_too_long",
            row,
            "VariantName cannot exceed 150 characters.");

    public static Error VariantCodeRequired(int row) =>
        RowError(
            "catalog_import.variant_code_required",
            row,
            "VariantCode is required.");

    public static Error VariantCodeTooLong(int row) =>
        RowError(
            "catalog_import.variant_code_too_long",
            row,
            "VariantCode cannot exceed 64 characters.");

    public static Error InvalidVariantCode(int row) =>
        RowError(
            "catalog_import.invalid_variant_code",
            row,
            "VariantCode may contain only letters, numbers, hyphens, underscores, and periods.");

    public static Error InvalidGtin(int row) =>
        RowError(
            "catalog_import.invalid_gtin",
            row,
            "Gtin must contain 8, 12, 13, or 14 digits after spaces and hyphens are removed.");

    public static Error InconsistentProductField(
        string productKey,
        string field,
        int row) =>
        RowError(
            "catalog_import.inconsistent_product",
            row,
            $"{field} does not match the other rows for ProductKey '{productKey}'.");

    public static Error TooManyVariants(
        string productKey,
        int count)
    {
        return new Error(
            "catalog_import.too_many_variants",
            $"ProductKey '{productKey}' contains {count} variants; the maximum is 100.");
    }

    public static Error DuplicateVariantCode(
        string productKey,
        string variantCode,
        int row) =>
        RowError(
            "catalog_import.duplicate_variant_code",
            row,
            $"VariantCode '{variantCode}' appears more than once for ProductKey '{productKey}'.");

    public static Error DuplicateGtin(
        string gtin,
        int row) =>
        RowError(
            "catalog_import.duplicate_gtin",
            row,
            $"Gtin '{gtin}' appears more than once in this file.");

    public static Error GtinAlreadyExists(string gtin)
    {
        return new Error(
            GtinConflictCode,
            $"Gtin '{gtin}' already belongs to another product variant.");
    }

    public static readonly Error ConcurrentGtinConflict = new(
        GtinConflictCode,
        "A supplied GTIN was assigned by another request. Refresh and try again.");

    private static Error RowError(
        string code,
        int row,
        string description)
    {
        return new Error(
            code,
            $"CSV row {row}: {description}");
    }
}
