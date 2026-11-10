namespace ECommerce.AI.Api;

public sealed class InMemoryProductIndex(
    EmbeddingService embeddings,
    StorefrontCatalogClient storefront)
{
    private IndexedProduct[] _products = [];

    public int Count => Volatile.Read(ref _products).Length;

    public async Task<int> ReindexAsync(
        CancellationToken cancellationToken)
    {
        var listings = await storefront.GetAllAsync(cancellationToken);
        var indexedProducts = new List<IndexedProduct>(listings.Count);

        foreach (var listing in listings)
        {
            var content = BuildContent(listing);
            var vector = await embeddings.CreateAsync(
                content,
                cancellationToken);

            indexedProducts.Add(new IndexedProduct(
                new ProductCard(
                    listing.ListingId,
                    listing.ProductId,
                    listing.ProductTitle,
                    listing.BrandName,
                    listing.Description ?? string.Empty,
                    listing.VariantName,
                    listing.PriceAmount,
                    listing.CurrencyCode,
                    listing.AvailableQuantity),
                content.ToLowerInvariant(),
                vector));
        }

        Volatile.Write(ref _products, indexedProducts.ToArray());
        return indexedProducts.Count;
    }

    public async Task<IReadOnlyList<ProductCard>> SearchAsync(
        string query,
        int take,
        IReadOnlyCollection<string>? excludedListingIds,
        CancellationToken cancellationToken)
    {
        var products = Volatile.Read(ref _products);

        if (products.Length == 0)
        {
            throw new InvalidOperationException(
                "The local product recommendation index is empty. " +
                "Run POST /api/ai/admin/reindex after starting the AI API.");
        }

        query = string.IsNullOrWhiteSpace(query)
            ? "useful in-stock products"
            : query.Trim();

        take = Math.Clamp(take, 1, 12);

        var queryVector = await embeddings.CreateAsync(
            query,
            cancellationToken);

        var queryTerms = query
            .ToLowerInvariant()
            .Split(
                [' ', '\r', '\n', '\t', ',', '.', ':', ';', '-', '_'],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Where(term => term.Length >= 2)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var excluded = excludedListingIds is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(
                excludedListingIds,
                StringComparer.OrdinalIgnoreCase);

        return products
            .Where(product =>
                product.Card.AvailableQuantity > 0 &&
                !excluded.Contains(product.Card.ListingId))
            .Select(product => new
            {
                product.Card,
                Score =
                    (0.85 * CosineSimilarity(
                        queryVector,
                        product.Vector)) +
                    (0.15 * LexicalScore(
                        queryTerms,
                        product.NormalizedContent))
            })
            .OrderByDescending(result => result.Score)
            .Take(take)
            .Select(result => result.Card)
            .ToArray();
    }

    private static double CosineSimilarity(
        IReadOnlyList<float> left,
        IReadOnlyList<float> right)
    {
        if (left.Count != right.Count || left.Count == 0)
        {
            return 0;
        }

        double dotProduct = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;

        for (var index = 0; index < left.Count; index++)
        {
            dotProduct += left[index] * right[index];
            leftMagnitude += left[index] * left[index];
            rightMagnitude += right[index] * right[index];
        }

        if (leftMagnitude == 0 || rightMagnitude == 0)
        {
            return 0;
        }

        return dotProduct /
            (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }

    private static double LexicalScore(
        IReadOnlyCollection<string> queryTerms,
        string normalizedContent)
    {
        if (queryTerms.Count == 0)
        {
            return 0;
        }

        var matches = queryTerms.Count(normalizedContent.Contains);
        return (double)matches / queryTerms.Count;
    }

    private static string BuildContent(StorefrontListing listing)
    {
        return $"""
            Product: {listing.ProductTitle}
            Brand: {listing.BrandName}
            Description: {listing.Description}
            Variant: {listing.VariantName}
            Variant code: {listing.VariantCode}
            Seller: {listing.SellerDisplayName}
            Price: {listing.PriceAmount} {listing.CurrencyCode}
            Available quantity: {listing.AvailableQuantity}
            """;
    }

    private sealed record IndexedProduct(
        ProductCard Card,
        string NormalizedContent,
        float[] Vector);
}
