using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Listings.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class SellerListingRepository(
    ECommerceDbContext dbContext)
    : ISellerListingRepository
{
    public async Task<SellerStatus?> GetSellerStatusAsync(
        Guid sellerId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Sellers
            .AsNoTracking()
            .Where(seller => seller.Id == sellerId)
            .Select(seller => (SellerStatus?)seller.Status)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SellerListingVariantSnapshot?>
        GetVariantAsync(
            Guid productVariantId,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductVariants
            .AsNoTracking()
            .Where(variant =>
                variant.Id == productVariantId)
            .Select(variant =>
                new SellerListingVariantSnapshot(
                    variant.ProductId,
                    variant.Id,
                    variant.Product.Title,
                    variant.Product.BrandName,
                    variant.Name,
                    variant.VariantCode,
                    variant.Product.Status,
                    variant.Status))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SellerListingPage>
    GetForSellerAsync(
        Guid sellerId,
        SellerListingStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (sellerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Seller ID is required.",
                nameof(sellerId));
        }

        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skip));
        }

        if (take is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(take));
        }

        var query = dbContext.SellerListings
            .AsNoTracking()
            .Where(listing =>
                listing.SellerId == sellerId);

        if (status.HasValue)
        {
            query = query.Where(listing =>
                listing.Status == status.Value);
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var items = await query
            .OrderByDescending(listing =>
                listing.CreatedAtUtc)
            .ThenByDescending(listing =>
                listing.Id)
            .Skip(skip)
            .Take(take)
            .Select(listing =>
                new SellerListingReadModel(
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
                    listing.Status,
                    listing.RowVersion,
                    listing.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return new SellerListingPage(
            items,
            totalCount);
    }

    public async Task<SellerListingReadModel?>
    FindByIdAsync(
        Guid sellerId,
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SellerListings
            .AsNoTracking()
            .Where(listing =>
                listing.SellerId == sellerId &&
                listing.Id == listingId)
            .Select(listing =>
                new SellerListingReadModel(
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
                    listing.Status,
                    listing.RowVersion,
                    listing.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SellerListingCreateOutcome>
        TryCreateAsync(
            SellerListing listing,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(listing);

        dbContext.SellerListings.Add(listing);

        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);

            return SellerListingCreateOutcome.Created;
        }
        catch (DbUpdateException exception)
            when (IsUniqueConflict(
                exception,
                "IX_SellerListings_SellerId_NormalizedSellerSku"))
        {
            dbContext.ChangeTracker.Clear();

            return SellerListingCreateOutcome
                .DuplicateSellerSku;
        }
        catch (DbUpdateException exception)
            when (IsUniqueConflict(
                exception,
                "IX_SellerListings_SellerId_ProductVariantId"))
        {
            dbContext.ChangeTracker.Clear();

            return SellerListingCreateOutcome
                .DuplicateSellerVariant;
        }
    }

    private static bool IsUniqueConflict(
        DbUpdateException exception,
        string indexName)
    {
        return exception.InnerException
                   is SqlException sqlException &&
               sqlException.Number is 2601 or 2627 &&
               sqlException.Message.Contains(
                   indexName,
                   StringComparison.OrdinalIgnoreCase);
    }
}