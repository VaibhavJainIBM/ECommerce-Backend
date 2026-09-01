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