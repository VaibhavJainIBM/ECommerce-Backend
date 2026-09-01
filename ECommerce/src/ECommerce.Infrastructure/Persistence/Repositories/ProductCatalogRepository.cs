using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class ProductCatalogRepository(
    ECommerceDbContext dbContext)
    : IProductCatalogRepository
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

    public Task<Product?> GetWithVariantsAsync(
    Guid productId,
    CancellationToken cancellationToken = default)
    {
        return dbContext.Products
            .Include(product => product.Variants)
            .SingleOrDefaultAsync(
                product => product.Id == productId,
                cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<bool> TryCreateAsync(
        Product product,
        IReadOnlyCollection<ProductVariant> variants,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        ArgumentNullException.ThrowIfNull(variants);

        if (variants.Count == 0)
        {
            throw new ArgumentException(
                "At least one variant is required.",
                nameof(variants));
        }

        if (variants.Any(variant =>
                variant.ProductId != product.Id))
        {
            throw new InvalidOperationException(
                "Every variant must belong to the product.");
        }

        dbContext.Products.Add(product);

        dbContext.ProductVariants.AddRange(variants);

        try
        {
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