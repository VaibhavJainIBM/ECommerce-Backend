using ECommerce.Application.Abstractions.Files;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Catalog.Importing;

public sealed class AdminCatalogImportService(
    ICatalogCsvParser csvParser,
    ICatalogBulkRepository repository)
    : IAdminCatalogImportService
{
    private const int MaximumValidationErrors = 100;
    private const int MaximumVariantsPerProduct = 100;

    public async Task<Result<CatalogImportResponseDto>> ImportAsync(
        Stream csvStream,
        bool activate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(csvStream);

        var parsed = await csvParser.ParseAsync(
            csvStream,
            cancellationToken);

        if (parsed.Errors.Count > 0)
        {
            return Result<CatalogImportResponseDto>.Failure(
                parsed.Errors);
        }

        var rows = parsed.Rows
            .OrderBy(row => row.RowNumber)
            .ToArray();

        if (rows.Length == 0)
        {
            return Result<CatalogImportResponseDto>.Failure(
                CatalogImportErrors.NoRows);
        }

        var errors = new List<Error>();
        var validProductKeys =
            new Dictionary<int, string>();
        var gtins = new HashSet<string>(
            StringComparer.Ordinal);

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var productKey = NormalizeCode(
                row.ProductKey);

            if (productKey is null)
            {
                AddError(
                    errors,
                    CatalogImportErrors.ProductKeyRequired(
                        row.RowNumber));
            }
            else if (productKey.Length > 64)
            {
                AddError(
                    errors,
                    CatalogImportErrors.ProductKeyTooLong(
                        row.RowNumber));
            }
            else if (productKey.Any(character =>
                         !IsAllowedCodeCharacter(character)))
            {
                AddError(
                    errors,
                    CatalogImportErrors.InvalidProductKey(
                        row.RowNumber));
            }
            else
            {
                validProductKeys[row.RowNumber] =
                    productKey;
            }

            ValidateRequiredText(
                row.Title,
                250,
                () => CatalogImportErrors.TitleRequired(
                    row.RowNumber),
                () => CatalogImportErrors.TitleTooLong(
                    row.RowNumber),
                errors);

            ValidateRequiredText(
                row.BrandName,
                150,
                () => CatalogImportErrors.BrandNameRequired(
                    row.RowNumber),
                () => CatalogImportErrors.BrandNameTooLong(
                    row.RowNumber),
                errors);

            if (NormalizeOptional(row.Description)?.Length >
                4000)
            {
                AddError(
                    errors,
                    CatalogImportErrors.DescriptionTooLong(
                        row.RowNumber));
            }

            ValidateRequiredText(
                row.VariantName,
                150,
                () =>
                    CatalogImportErrors.VariantNameRequired(
                        row.RowNumber),
                () =>
                    CatalogImportErrors.VariantNameTooLong(
                        row.RowNumber),
                errors);

            var variantCode = NormalizeCode(
                row.VariantCode);

            if (variantCode is null)
            {
                AddError(
                    errors,
                    CatalogImportErrors.VariantCodeRequired(
                        row.RowNumber));
            }
            else if (variantCode.Length > 64)
            {
                AddError(
                    errors,
                    CatalogImportErrors.VariantCodeTooLong(
                        row.RowNumber));
            }
            else if (variantCode.Any(character =>
                         !IsAllowedCodeCharacter(character)))
            {
                AddError(
                    errors,
                    CatalogImportErrors.InvalidVariantCode(
                        row.RowNumber));
            }

            var gtin = NormalizeGtin(row.Gtin);

            if (gtin is null)
            {
                continue;
            }

            if (!IsValidGtin(gtin))
            {
                AddError(
                    errors,
                    CatalogImportErrors.InvalidGtin(
                        row.RowNumber));
            }
            else if (!gtins.Add(gtin))
            {
                AddError(
                    errors,
                    CatalogImportErrors.DuplicateGtin(
                        gtin,
                        row.RowNumber));
            }
        }

        var groups = rows
            .Where(row =>
                validProductKeys.ContainsKey(
                    row.RowNumber))
            .GroupBy(
                row => validProductKeys[row.RowNumber],
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Min(row =>
                row.RowNumber))
            .ToArray();

        foreach (var group in groups)
        {
            ValidateProductGroup(
                group.Key,
                group.OrderBy(row => row.RowNumber)
                    .ToArray(),
                errors);
        }

        if (errors.Count > 0)
        {
            return Result<CatalogImportResponseDto>.Failure(
                errors);
        }

        var normalizedGtins = rows
            .Select(row => NormalizeGtin(row.Gtin))
            .Where(gtin => gtin is not null)
            .Select(gtin => gtin!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalizedGtins.Length > 0)
        {
            var existingGtins =
                await repository.FindExistingGtinsAsync(
                    normalizedGtins,
                    cancellationToken);

            if (existingGtins.Count > 0)
            {
                return Result<CatalogImportResponseDto>.Failure(
                    existingGtins
                        .OrderBy(
                            gtin => gtin,
                            StringComparer.Ordinal)
                        .Select(
                            CatalogImportErrors
                                .GtinAlreadyExists));
            }
        }

        var products = new List<Product>();
        var variants = new List<ProductVariant>();
        var imports = new List<ImportedProduct>();

        foreach (var group in groups)
        {
            var groupRows = group
                .OrderBy(row => row.RowNumber)
                .ToArray();
            var first = groupRows[0];
            var product = new Product(
                first.Title!.Trim(),
                first.BrandName!.Trim(),
                NormalizeOptional(first.Description));
            var importedVariants =
                new List<ImportedVariant>();

            foreach (var row in groupRows)
            {
                var variant = new ProductVariant(
                    product.Id,
                    row.VariantName!.Trim(),
                    row.VariantCode!.Trim(),
                    NormalizeOptional(row.Gtin));

                if (activate)
                {
                    variant.Activate();
                }

                variants.Add(variant);
                importedVariants.Add(
                    new ImportedVariant(
                        row.RowNumber,
                        variant));
            }

            if (activate)
            {
                product.Activate();
            }

            products.Add(product);
            imports.Add(
                new ImportedProduct(
                    group.Key,
                    product,
                    importedVariants));
        }

        var created =
            await repository.TryCreateBatchAsync(
                products,
                variants,
                cancellationToken);

        if (!created)
        {
            return Result<CatalogImportResponseDto>.Failure(
                CatalogImportErrors
                    .ConcurrentGtinConflict);
        }

        var response = new CatalogImportResponseDto(
            rows.Length,
            products.Count,
            variants.Count,
            activate,
            imports
                .Select(import =>
                    new CatalogImportedProductDto(
                        import.ProductKey,
                        import.Product.Id,
                        import.Product.Status.ToString(),
                        import.Variants
                            .Select(item =>
                                new CatalogImportedVariantDto(
                                    item.RowNumber,
                                    item.Variant.Id,
                                    item.Variant.Name,
                                    item.Variant.VariantCode,
                                    item.Variant.Gtin,
                                    item.Variant.Status
                                        .ToString()))
                            .ToArray()))
                .ToArray());

        return Result<CatalogImportResponseDto>.Success(
            response);
    }

    private static void ValidateProductGroup(
        string productKey,
        IReadOnlyCollection<CatalogCsvRow> rows,
        ICollection<Error> errors)
    {
        if (rows.Count > MaximumVariantsPerProduct)
        {
            AddError(
                errors,
                CatalogImportErrors.TooManyVariants(
                    productKey,
                    rows.Count));
        }

        var first = rows.First();
        var expectedTitle =
            NormalizeRequired(first.Title);
        var expectedBrand =
            NormalizeRequired(first.BrandName);
        var expectedDescription =
            NormalizeOptional(first.Description);
        var variantCodes = new HashSet<string>(
            StringComparer.Ordinal);

        foreach (var row in rows)
        {
            if (!string.Equals(
                    NormalizeRequired(row.Title),
                    expectedTitle,
                    StringComparison.Ordinal))
            {
                AddError(
                    errors,
                    CatalogImportErrors
                        .InconsistentProductField(
                            productKey,
                            "Title",
                            row.RowNumber));
            }

            if (!string.Equals(
                    NormalizeRequired(row.BrandName),
                    expectedBrand,
                    StringComparison.Ordinal))
            {
                AddError(
                    errors,
                    CatalogImportErrors
                        .InconsistentProductField(
                            productKey,
                            "BrandName",
                            row.RowNumber));
            }

            if (!string.Equals(
                    NormalizeOptional(row.Description),
                    expectedDescription,
                    StringComparison.Ordinal))
            {
                AddError(
                    errors,
                    CatalogImportErrors
                        .InconsistentProductField(
                            productKey,
                            "Description",
                            row.RowNumber));
            }

            var variantCode = NormalizeCode(
                row.VariantCode);

            if (variantCode is not null &&
                variantCode.Length <= 64 &&
                variantCode.All(
                    IsAllowedCodeCharacter) &&
                !variantCodes.Add(variantCode))
            {
                AddError(
                    errors,
                    CatalogImportErrors
                        .DuplicateVariantCode(
                            productKey,
                            variantCode,
                            row.RowNumber));
            }
        }
    }

    private static void ValidateRequiredText(
        string? value,
        int maximumLength,
        Func<Error> requiredError,
        Func<Error> tooLongError,
        ICollection<Error> errors)
    {
        var normalized = NormalizeRequired(value);

        if (normalized is null)
        {
            AddError(errors, requiredError());
        }
        else if (normalized.Length > maximumLength)
        {
            AddError(errors, tooLongError());
        }
    }

    private static void AddError(
        ICollection<Error> errors,
        Error error)
    {
        if (errors.Count < MaximumValidationErrors)
        {
            errors.Add(error);
        }
    }

    private static string? NormalizeRequired(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string? NormalizeCode(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
    }

    private static string? NormalizeGtin(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value
                .Trim()
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty);
    }

    private static bool IsValidGtin(string gtin)
    {
        return gtin.Length is 8 or 12 or 13 or 14 &&
               gtin.All(character =>
                   character is >= '0' and <= '9');
    }

    private static bool IsAllowedCodeCharacter(
        char character)
    {
        return character is >= 'A' and <= 'Z' ||
               character is >= '0' and <= '9' ||
               character is '-' or '_' or '.';
    }

    private sealed record ImportedProduct(
        string ProductKey,
        Product Product,
        IReadOnlyCollection<ImportedVariant> Variants);

    private sealed record ImportedVariant(
        int RowNumber,
        ProductVariant Variant);
}
