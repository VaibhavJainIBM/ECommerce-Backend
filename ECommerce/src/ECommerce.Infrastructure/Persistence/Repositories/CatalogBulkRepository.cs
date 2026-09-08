using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class CatalogBulkRepository(
    ECommerceDbContext dbContext)
    : ICatalogBulkRepository
{
    public async Task<IReadOnlyCollection<string>>
        FindExistingGtinsAsync(
            IReadOnlyCollection<string> normalizedGtins,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            normalizedGtins);

        var gtins = normalizedGtins
            .Where(gtin =>
                !string.IsNullOrWhiteSpace(gtin))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (gtins.Length == 0)
        {
            return Array.Empty<string>();
        }

        return await dbContext.ProductVariants
            .AsNoTracking()
            .Where(variant =>
                variant.Gtin != null &&
                gtins.Contains(variant.Gtin))
            .Select(variant => variant.Gtin!)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> TryCreateBatchAsync(
        IReadOnlyCollection<Product> products,
        IReadOnlyCollection<ProductVariant> variants,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(variants);

        if (products.Count == 0 ||
            variants.Count == 0)
        {
            throw new ArgumentException(
                "The import batch must contain products and variants.");
        }

        var productIds = products
            .Select(product => product.Id)
            .ToHashSet();

        if (variants.Any(variant =>
                !productIds.Contains(variant.ProductId)))
        {
            throw new InvalidOperationException(
                "Every imported variant must belong to an imported product.");
        }

        dbContext.Products.AddRange(products);
        dbContext.ProductVariants.AddRange(variants);

        try
        {
            // The single SaveChanges call makes the whole import
            // succeed or roll back as one database transaction.
            await dbContext.SaveChangesAsync(
                cancellationToken);

            return true;
        }
        catch (DbUpdateException exception)
            when (IsGtinUniqueConflict(exception))
        {
            dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    private static bool IsGtinUniqueConflict(
        DbUpdateException exception)
    {
        return exception.InnerException
                   is SqlException sqlException &&
               sqlException.Number is 2601 or 2627 &&
               sqlException.Message.Contains(
                   "IX_ProductVariants_Gtin",
                   StringComparison.OrdinalIgnoreCase);
    }
}
