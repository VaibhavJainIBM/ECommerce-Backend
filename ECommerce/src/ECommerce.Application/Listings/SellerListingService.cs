using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common;
using ECommerce.Application.Listings.Dtos;
using ECommerce.Application.Listings.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Application.Listings;

public sealed class SellerListingService(
    ISellerListingRepository repository)
    : ISellerListingService
{
    private const decimal MaximumPrice =
        9_999_999_999_999_999.99m;

    public async Task<Result<SellerListingResponseDto>>
        CreateAsync(
            Guid sellerId,
            CreateSellerListingRequestDto? request,
            CancellationToken cancellationToken = default)
    {
        var validationErrors = Validate(sellerId, request);

        if (validationErrors.Count > 0)
        {
            return Result<SellerListingResponseDto>.Failure(
                validationErrors);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var sellerStatus =
            await repository.GetSellerStatusAsync(
                sellerId,
                cancellationToken);

        if (!sellerStatus.HasValue)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.SellerNotFound);
        }

        if (!CanCreateDraftListing(sellerStatus.Value))
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.SellerUnavailable(
                    sellerStatus.Value.ToString()));
        }

        var variant = await repository.GetVariantAsync(
            request!.ProductVariantId,
            cancellationToken);

        if (variant is null)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.VariantNotFound);
        }

        if (variant.ProductStatus != ProductStatus.Active)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.ProductNotActive);
        }

        if (variant.VariantStatus !=
            ProductVariantStatus.Active)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.VariantNotActive);
        }

        var price = new Money(
            request.PriceAmount,
            request.CurrencyCode!.Trim());

        var listing = new SellerListing(
            sellerId,
            request.ProductVariantId,
            request.SellerSku!.Trim(),
            price);

        var outcome = await repository.TryCreateAsync(
            listing,
            cancellationToken);

        if (outcome ==
            SellerListingCreateOutcome.DuplicateSellerSku)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.DuplicateSellerSku);
        }

        if (outcome ==
            SellerListingCreateOutcome.DuplicateSellerVariant)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.DuplicateSellerVariant);
        }

        var response = new SellerListingResponseDto(
            listing.Id,
            listing.SellerId,
            variant.ProductId,
            variant.ProductTitle,
            variant.BrandName,
            listing.ProductVariantId,
            variant.VariantName,
            variant.VariantCode,
            listing.SellerSku,
            listing.Price.Amount,
            listing.Price.CurrencyCode,
            listing.Status.ToString(),
            Convert.ToBase64String(listing.RowVersion),
            listing.CreatedAtUtc);

        return Result<SellerListingResponseDto>.Success(
            response);
    }

    private static bool CanCreateDraftListing(
        SellerStatus status)
    {
        return status is
            SellerStatus.PendingVerification or
            SellerStatus.UnderReview or
            SellerStatus.Rejected or
            SellerStatus.Active;
    }

    private static List<Error> Validate(
        Guid sellerId,
        CreateSellerListingRequestDto? request)
    {
        var errors = new List<Error>();

        if (sellerId == Guid.Empty)
        {
            errors.Add(
                SellerListingErrors.SellerIdRequired);
        }

        if (request is null)
        {
            errors.Add(
                SellerListingErrors.RequestRequired);

            return errors;
        }

        if (request.ProductVariantId == Guid.Empty)
        {
            errors.Add(
                SellerListingErrors.VariantIdRequired);
        }

        ValidateSellerSku(request.SellerSku, errors);
        ValidatePrice(request.PriceAmount, errors);
        ValidateCurrency(request.CurrencyCode, errors);

        return errors;
    }

    private static void ValidateSellerSku(
        string? sellerSku,
        ICollection<Error> errors)
    {
        if (string.IsNullOrWhiteSpace(sellerSku))
        {
            errors.Add(
                SellerListingErrors.SellerSkuRequired);

            return;
        }

        var normalizedSku = sellerSku.Trim();

        if (normalizedSku.Length > 64)
        {
            errors.Add(
                SellerListingErrors.SellerSkuTooLong);

            return;
        }

        if (normalizedSku.Any(character =>
                !IsAllowedSkuCharacter(character)))
        {
            errors.Add(
                SellerListingErrors.SellerSkuInvalid);
        }
    }

    private static void ValidatePrice(
        decimal price,
        ICollection<Error> errors)
    {
        if (price <= 0)
        {
            errors.Add(
                SellerListingErrors.PriceMustBePositive);
        }
        else if (price > MaximumPrice)
        {
            errors.Add(
                SellerListingErrors.PriceTooLarge);
        }
        else if (decimal.Round(price, 2) != price)
        {
            errors.Add(
                SellerListingErrors.PriceTooPrecise);
        }
    }

    private static void ValidateCurrency(
        string? currencyCode,
        ICollection<Error> errors)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            errors.Add(
                SellerListingErrors.CurrencyRequired);

            return;
        }

        var normalizedCurrency =
            currencyCode.Trim().ToUpperInvariant();

        if (normalizedCurrency.Length != 3 ||
            normalizedCurrency.Any(character =>
                character < 'A' ||
                character > 'Z'))
        {
            errors.Add(
                SellerListingErrors.CurrencyInvalid);
        }
    }

    private static bool IsAllowedSkuCharacter(
        char character)
    {
        return character is >= 'A' and <= 'Z' ||
               character is >= 'a' and <= 'z' ||
               character is >= '0' and <= '9' ||
               character is '-' or '_' or '.';
    }
}