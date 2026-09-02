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


    public async Task<Result<SellerListingResponseDto>>
    SubmitForReviewAsync(
        Guid sellerId,
        Guid listingId,
        ChangeSellerListingStatusRequestDto? request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<Error>();

        ValidateListingIds(
            sellerId,
            listingId,
            errors);

        if (request is null)
        {
            errors.Add(
                SellerListingErrors.StatusChangeRequestRequired);
        }
        else
        {
            ValidateRowVersion(
                request.RowVersion,
                errors);
        }

        if (errors.Count > 0)
        {
            return Result<SellerListingResponseDto>.Failure(
                errors);
        }

        var sellerStatus =
            await repository.GetSellerStatusAsync(
                sellerId,
                cancellationToken);

        if (!sellerStatus.HasValue)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.SellerNotFound);
        }

        if (sellerStatus.Value != SellerStatus.Active)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.SellerCannotPublish(
                    sellerStatus.Value.ToString()));
        }

        var listing = await repository.GetTrackedAsync(
            sellerId,
            listingId,
            cancellationToken);

        if (listing is null)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.ListingNotFound(listingId));
        }

        if (listing.Status is not
            (SellerListingStatus.Draft or
             SellerListingStatus.Rejected))
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.StatusChangeNotAllowed(
                    "submitted for review",
                    listing.Status.ToString()));
        }

        var expectedRowVersion =
            Convert.FromBase64String(
                request!.RowVersion!.Trim());

        listing.SubmitForReview();

        var outcome =
            await repository.SaveWithConcurrencyAsync(
                listing,
                expectedRowVersion,
                cancellationToken);

        if (outcome ==
            SellerListingSaveOutcome.ConcurrencyConflict)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.ConcurrencyConflict);
        }

        return Result<SellerListingResponseDto>.Success(
            Map(listing));
    }

    public async Task<Result<SellerListingResponseDto>>
        ApproveAsync(
            Guid sellerId,
            Guid listingId,
            ChangeSellerListingStatusRequestDto? request,
            CancellationToken cancellationToken = default)
    {
        var errors = new List<Error>();

        ValidateListingIds(
            sellerId,
            listingId,
            errors);

        if (request is null)
        {
            errors.Add(
                SellerListingErrors.StatusChangeRequestRequired);
        }
        else
        {
            ValidateRowVersion(
                request.RowVersion,
                errors);
        }

        if (errors.Count > 0)
        {
            return Result<SellerListingResponseDto>.Failure(
                errors);
        }

        var sellerStatus =
            await repository.GetSellerStatusAsync(
                sellerId,
                cancellationToken);

        if (!sellerStatus.HasValue)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.SellerNotFound);
        }

        if (sellerStatus.Value != SellerStatus.Active)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.SellerCannotPublish(
                    sellerStatus.Value.ToString()));
        }

        var listing = await repository.GetTrackedAsync(
            sellerId,
            listingId,
            cancellationToken);

        if (listing is null)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.ListingNotFound(listingId));
        }

        if (listing.Status !=
            SellerListingStatus.PendingReview)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.StatusChangeNotAllowed(
                    "approved",
                    listing.Status.ToString()));
        }

        var expectedRowVersion =
            Convert.FromBase64String(
                request!.RowVersion!.Trim());

        listing.Approve();

        var outcome =
            await repository.SaveWithConcurrencyAsync(
                listing,
                expectedRowVersion,
                cancellationToken);

        if (outcome ==
            SellerListingSaveOutcome.ConcurrencyConflict)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.ConcurrencyConflict);
        }

        return Result<SellerListingResponseDto>.Success(
            Map(listing));
    }


    public async Task<Result<SellerListingResponseDto>>
    UpdatePriceAsync(
        Guid sellerId,
        Guid listingId,
        UpdateSellerListingPriceRequestDto? request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<Error>();

        ValidateListingIds(
            sellerId,
            listingId,
            errors);

        if (request is null)
        {
            errors.Add(
                SellerListingErrors.PriceUpdateRequestRequired);
        }
        else
        {
            ValidatePrice(
                request.PriceAmount,
                errors);

            ValidateCurrency(
                request.CurrencyCode,
                errors);

            ValidateRowVersion(
                request.RowVersion,
                errors);
        }

        if (errors.Count > 0)
        {
            return Result<SellerListingResponseDto>.Failure(
                errors);
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

        if (!CanSellerChangePrice(
                sellerStatus.Value))
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.SellerCannotChangePrice(
                    sellerStatus.Value.ToString()));
        }

        var listing = await repository.GetTrackedAsync(
            sellerId,
            listingId,
            cancellationToken);

        if (listing is null)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.ListingNotFound(
                    listingId));
        }

        if (!listing.CanChangePrice)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.PriceChangeNotAllowed(
                    listing.Status.ToString()));
        }

        var price = new Money(
            request!.PriceAmount,
            request.CurrencyCode!.Trim());

        var expectedRowVersion =
            Convert.FromBase64String(
                request.RowVersion!.Trim());

        listing.ChangePrice(price);

        var outcome =
            await repository.SaveWithConcurrencyAsync(
                listing,
                expectedRowVersion,
                cancellationToken);

        if (outcome ==
            SellerListingSaveOutcome.ConcurrencyConflict)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.ConcurrencyConflict);
        }

        return Result<SellerListingResponseDto>.Success(
            Map(listing));
    }


    public async Task<Result<SellerListingResponseDto>>
    ArchiveAsync(
        Guid sellerId,
        Guid listingId,
        ArchiveSellerListingRequestDto? request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<Error>();

        ValidateListingIds(
            sellerId,
            listingId,
            errors);

        if (request is null)
        {
            errors.Add(
                SellerListingErrors.ArchiveRequestRequired);
        }
        else
        {
            ValidateRowVersion(
                request.RowVersion,
                errors);
        }

        if (errors.Count > 0)
        {
            return Result<SellerListingResponseDto>.Failure(
                errors);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var listing = await repository.GetTrackedAsync(
            sellerId,
            listingId,
            cancellationToken);

        if (listing is null)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.ListingNotFound(
                    listingId));
        }

        // Archive is idempotent. Calling it again returns
        // the current archived representation.
        if (listing.Status ==
            SellerListingStatus.Archived)
        {
            return Result<SellerListingResponseDto>.Success(
                Map(listing));
        }

        var expectedRowVersion =
            Convert.FromBase64String(
                request!.RowVersion!.Trim());

        listing.Archive();

        var outcome =
            await repository.SaveWithConcurrencyAsync(
                listing,
                expectedRowVersion,
                cancellationToken);

        if (outcome ==
            SellerListingSaveOutcome.ConcurrencyConflict)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.ConcurrencyConflict);
        }

        return Result<SellerListingResponseDto>.Success(
            Map(listing));
    }

    public async Task<Result<PagedSellerListingsResponseDto>>
    GetForSellerAsync(
        Guid sellerId,
        SellerListingQueryDto? query,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<Error>();

        if (sellerId == Guid.Empty)
        {
            errors.Add(
                SellerListingErrors.SellerIdRequired);
        }

        var page = query?.Page ?? 1;
        var pageSize = query?.PageSize ?? 20;

        if (page < 1)
        {
            errors.Add(
                SellerListingErrors.PageInvalid);
        }

        if (pageSize is < 1 or > 100)
        {
            errors.Add(
                SellerListingErrors.PageSizeInvalid);
        }

        SellerListingStatus? status = null;

        if (!string.IsNullOrWhiteSpace(query?.Status))
        {
            var suppliedStatus = query.Status.Trim();

            if (!Enum.TryParse<SellerListingStatus>(
                    suppliedStatus,
                    ignoreCase: true,
                    out var parsedStatus) ||
                !Enum.IsDefined(
                    typeof(SellerListingStatus),
                    parsedStatus))
            {
                errors.Add(
                    SellerListingErrors.InvalidStatus(
                        suppliedStatus));
            }
            else
            {
                status = parsedStatus;
            }
        }

        var skipAsLong =
            ((long)page - 1) * pageSize;

        if (skipAsLong > int.MaxValue)
        {
            errors.Add(
                SellerListingErrors.PaginationTooDeep);
        }

        if (errors.Count > 0)
        {
            return Result<PagedSellerListingsResponseDto>
                .Failure(errors);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var result = await repository.GetForSellerAsync(
            sellerId,
            status,
            (int)skipAsLong,
            pageSize,
            cancellationToken);

        var items = result.Items
            .Select(Map)
            .ToArray();

        var totalPages = result.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(
                result.TotalCount /
                (double)pageSize);

        var response =
            new PagedSellerListingsResponseDto(
                items,
                page,
                pageSize,
                result.TotalCount,
                totalPages);

        return Result<PagedSellerListingsResponseDto>
            .Success(response);
    }

    public async Task<Result<SellerListingResponseDto>>
    GetByIdAsync(
        Guid sellerId,
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<Error>();

        if (sellerId == Guid.Empty)
        {
            errors.Add(
                SellerListingErrors.SellerIdRequired);
        }

        if (listingId == Guid.Empty)
        {
            errors.Add(
                SellerListingErrors.ListingIdRequired);
        }

        if (errors.Count > 0)
        {
            return Result<SellerListingResponseDto>.Failure(
                errors);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var listing = await repository.FindByIdAsync(
            sellerId,
            listingId,
            cancellationToken);

        if (listing is null)
        {
            return Result<SellerListingResponseDto>.Failure(
                SellerListingErrors.ListingNotFound(
                    listingId));
        }

        return Result<SellerListingResponseDto>.Success(
            Map(listing));
    }

    private static void ValidateRowVersion(
    string? rowVersion,
    ICollection<Error> errors)
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            errors.Add(
                SellerListingErrors.RowVersionRequired);

            return;
        }

        try
        {
            var bytes = Convert.FromBase64String(
                rowVersion.Trim());

            if (bytes.Length != 8)
            {
                errors.Add(
                    SellerListingErrors.RowVersionInvalid);
            }
        }
        catch (FormatException)
        {
            errors.Add(
                SellerListingErrors.RowVersionInvalid);
        }
    }


    private static SellerListingResponseDto Map(
    SellerListing listing)
    {
        return new SellerListingResponseDto(
            listing.Id,
            listing.SellerId,
            listing.ProductVariant.ProductId,
            listing.ProductVariant.Product.Title,
            listing.ProductVariant.Product.BrandName,
            listing.ProductVariantId,
            listing.ProductVariant.Name,
            listing.ProductVariant.VariantCode,
            listing.SellerSku,
            listing.Price.Amount,
            listing.Price.CurrencyCode,
            listing.Status.ToString(),
            Convert.ToBase64String(
                listing.RowVersion),
            listing.CreatedAtUtc);
    }

    private static bool CanSellerChangePrice(
    SellerStatus status)
    {
        return status is
            SellerStatus.PendingVerification or
            SellerStatus.UnderReview or
            SellerStatus.Rejected or
            SellerStatus.Active;
    }
    private static void ValidateListingIds(
    Guid sellerId,
    Guid listingId,
    ICollection<Error> errors)
    {
        if (sellerId == Guid.Empty)
        {
            errors.Add(
                SellerListingErrors.SellerIdRequired);
        }

        if (listingId == Guid.Empty)
        {
            errors.Add(
                SellerListingErrors.ListingIdRequired);
        }
    }
    private static SellerListingResponseDto Map(
    SellerListingReadModel listing)
    {
        return new SellerListingResponseDto(
            listing.ListingId,
            listing.SellerId,
            listing.ProductId,
            listing.ProductTitle,
            listing.BrandName,
            listing.ProductVariantId,
            listing.VariantName,
            listing.VariantCode,
            listing.SellerSku,
            listing.PriceAmount,
            listing.CurrencyCode,
            listing.Status.ToString(),
            Convert.ToBase64String(
                listing.RowVersion),
            listing.CreatedAtUtc);
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