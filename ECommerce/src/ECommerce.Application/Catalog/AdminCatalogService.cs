using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Catalog.Dtos;
using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Catalog;

public sealed class AdminCatalogService(
    IProductCatalogRepository repository)
    : IAdminCatalogService
{
    private const int MaximumVariantCount = 100;

    public async Task<Result<CreateProductResponseDto>>
        CreateProductAsync(
            CreateProductRequestDto? request,
            CancellationToken cancellationToken = default)
    {
        var validationErrors = Validate(request);

        if (validationErrors.Count > 0)
        {
            return Result<CreateProductResponseDto>.Failure(
                validationErrors);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var product = new Product(
            request!.Title!.Trim(),
            request.BrandName!.Trim(),
            NormalizeOptional(request.Description));

        var variants = request.Variants!
            .Select(variant => new ProductVariant(
                product.Id,
                variant!.Name!.Trim(),
                variant.VariantCode!.Trim(),
                NormalizeOptional(variant.Gtin)))
            .ToArray();

        var normalizedGtins = variants
            .Where(variant => variant.Gtin is not null)
            .Select(variant => variant.Gtin!)
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
                var errors = existingGtins
                    .OrderBy(
                        gtin => gtin,
                        StringComparer.Ordinal)
                    .Select(
                        CatalogErrors.GtinAlreadyExists)
                    .ToArray();

                return Result<CreateProductResponseDto>.Failure(
                    errors);
            }
        }

        var created = await repository.TryCreateAsync(
            product,
            variants,
            cancellationToken);

        if (!created)
        {
            return Result<CreateProductResponseDto>.Failure(
                CatalogErrors.ConcurrentGtinConflict);
        }

        var response = new CreateProductResponseDto(
            product.Id,
            product.Title,
            product.BrandName,
            product.Description,
            product.Status.ToString(),
            product.CreatedAtUtc,
            variants
                .Select(variant =>
                    new ProductVariantResponseDto(
                        variant.Id,
                        variant.Name,
                        variant.VariantCode,
                        variant.Gtin,
                        variant.Status.ToString(),
                        variant.CreatedAtUtc))
                .ToArray());

        return Result<CreateProductResponseDto>.Success(
            response);
    }

    public async Task<Result<CreateProductResponseDto>>
    ActivateProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var product = await repository.GetWithVariantsAsync(
            productId,
            cancellationToken);

        if (product is null)
        {
            return Result<CreateProductResponseDto>.Failure(
                CatalogErrors.ProductNotFound(productId));
        }

        var variants = product.Variants
            .OrderBy(
                variant => variant.VariantCode,
                StringComparer.Ordinal)
            .ToArray();

        // Calling activate twice is safe and returns
        // the current active product.
        if (product.Status == ProductStatus.Active)
        {
            return Result<CreateProductResponseDto>.Success(
                MapProduct(product, variants));
        }

        if (product.Status != ProductStatus.Draft)
        {
            return Result<CreateProductResponseDto>.Failure(
                CatalogErrors.ProductCannotBeActivated(
                    product.Status.ToString()));
        }

        if (variants.Length == 0)
        {
            return Result<CreateProductResponseDto>.Failure(
                CatalogErrors.ProductHasNoVariants);
        }

        var invalidVariant = variants.FirstOrDefault(
            variant =>
                variant.Status != ProductVariantStatus.Draft);

        if (invalidVariant is not null)
        {
            return Result<CreateProductResponseDto>.Failure(
                CatalogErrors.VariantCannotBeActivated(
                    invalidVariant.VariantCode,
                    invalidVariant.Status.ToString()));
        }

        // Activate every variant before activating
        // the parent product.
        foreach (var variant in variants)
        {
            variant.Activate();
        }

        product.Activate();

        // Product and all variants update atomically.
        await repository.SaveChangesAsync(
            cancellationToken);

        return Result<CreateProductResponseDto>.Success(
            MapProduct(product, variants));
    }

    private static List<Error> Validate(
        CreateProductRequestDto? request)
    {
        var errors = new List<Error>();

        if (request is null)
        {
            errors.Add(CatalogErrors.RequestRequired);
            return errors;
        }

        ValidateProductFields(request, errors);

        if (request.Variants is null ||
            request.Variants.Count == 0)
        {
            errors.Add(CatalogErrors.VariantsRequired);
            return errors;
        }

        if (request.Variants.Count > MaximumVariantCount)
        {
            errors.Add(CatalogErrors.TooManyVariants);
            return errors;
        }

        var variantCodes = new HashSet<string>(
            StringComparer.Ordinal);

        var gtins = new HashSet<string>(
            StringComparer.Ordinal);

        for (var index = 0;
             index < request.Variants.Count;
             index++)
        {
            var variant = request.Variants[index];

            if (variant is null)
            {
                errors.Add(
                    CatalogErrors.VariantRequired(index));

                continue;
            }

            ValidateVariantName(
                variant,
                index,
                errors);

            ValidateVariantCode(
                variant,
                index,
                variantCodes,
                errors);

            ValidateGtin(
                variant,
                index,
                gtins,
                errors);
        }

        return errors;
    }

    private static void ValidateProductFields(
        CreateProductRequestDto request,
        ICollection<Error> errors)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add(CatalogErrors.TitleRequired);
        }
        else if (request.Title.Trim().Length > 250)
        {
            errors.Add(CatalogErrors.TitleTooLong);
        }

        if (string.IsNullOrWhiteSpace(
                request.BrandName))
        {
            errors.Add(CatalogErrors.BrandNameRequired);
        }
        else if (
            request.BrandName.Trim().Length > 150)
        {
            errors.Add(CatalogErrors.BrandNameTooLong);
        }

        if (!string.IsNullOrWhiteSpace(
                request.Description) &&
            request.Description.Trim().Length > 4000)
        {
            errors.Add(
                CatalogErrors.DescriptionTooLong);
        }
    }

    private static void ValidateVariantName(
        CreateProductVariantRequestDto variant,
        int index,
        ICollection<Error> errors)
    {
        if (string.IsNullOrWhiteSpace(variant.Name))
        {
            errors.Add(
                CatalogErrors.VariantNameRequired(index));
        }
        else if (variant.Name.Trim().Length > 150)
        {
            errors.Add(
                CatalogErrors.VariantNameTooLong(index));
        }
    }

    private static void ValidateVariantCode(
        CreateProductVariantRequestDto variant,
        int index,
        ISet<string> variantCodes,
        ICollection<Error> errors)
    {
        if (string.IsNullOrWhiteSpace(
                variant.VariantCode))
        {
            errors.Add(
                CatalogErrors.VariantCodeRequired(index));

            return;
        }

        var normalizedVariantCode =
            variant.VariantCode
                .Trim()
                .ToUpperInvariant();

        if (normalizedVariantCode.Length > 64)
        {
            errors.Add(
                CatalogErrors.VariantCodeTooLong(index));

            return;
        }

        if (normalizedVariantCode.Any(character =>
                !IsAllowedCodeCharacter(character)))
        {
            errors.Add(
                CatalogErrors.InvalidVariantCode(index));

            return;
        }

        if (!variantCodes.Add(normalizedVariantCode))
        {
            errors.Add(
                CatalogErrors.DuplicateVariantCode(
                    normalizedVariantCode));
        }
    }

    private static void ValidateGtin(
        CreateProductVariantRequestDto variant,
        int index,
        ISet<string> gtins,
        ICollection<Error> errors)
    {
        if (string.IsNullOrWhiteSpace(variant.Gtin))
        {
            return;
        }

        var normalizedGtin =
            NormalizeGtin(variant.Gtin);

        if (!IsValidNormalizedGtin(normalizedGtin))
        {
            errors.Add(CatalogErrors.InvalidGtin(index));
            return;
        }

        if (!gtins.Add(normalizedGtin))
        {
            errors.Add(
                CatalogErrors.DuplicateGtinInRequest(
                    normalizedGtin));
        }
    }

    private static string NormalizeGtin(string gtin)
    {
        return gtin
            .Trim()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty);
    }

    private static bool IsValidNormalizedGtin(
        string gtin)
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

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }


    private static CreateProductResponseDto MapProduct(
    Product product,
    IEnumerable<ProductVariant> variants)
    {
        return new CreateProductResponseDto(
            product.Id,
            product.Title,
            product.BrandName,
            product.Description,
            product.Status.ToString(),
            product.CreatedAtUtc,
            variants
                .OrderBy(
                    variant => variant.VariantCode,
                    StringComparer.Ordinal)
                .Select(variant =>
                    new ProductVariantResponseDto(
                        variant.Id,
                        variant.Name,
                        variant.VariantCode,
                        variant.Gtin,
                        variant.Status.ToString(),
                        variant.CreatedAtUtc))
                .ToArray());
    }
}