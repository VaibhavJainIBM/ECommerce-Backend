using ECommerce.Application.Abstractions.Files;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Catalog.Importing;
using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.UnitTests;

public sealed class CatalogImportServiceTests
{
    [Fact]
    public async Task ParserError_IsReturnedWithoutRepositoryCalls()
    {
        var expected = new Error(
            "catalog_import.invalid_csv", "Invalid CSV.");
        var parser = new FakeParser(new(
            Array.Empty<CatalogCsvRow>(), new[] { expected }));
        var repository = new FakeRepository();

        using var stream = new MemoryStream();
        var result = await new AdminCatalogImportService(
            parser, repository).ImportAsync(stream, false);

        Assert.True(result.IsFailure);
        Assert.Equal(expected, Assert.Single(result.Errors));
        Assert.Equal(1, parser.Calls);
        Assert.Equal(0, repository.FindCalls);
        Assert.Equal(0, repository.WriteCalls);
    }

    [Fact]
    public async Task DuplicateVariantAndNormalizedGtin_AreRejectedBeforeRepository()
    {
        var rows = new[]
        {
            Row(2, "PHONE-1", "Black 128", "phone-128", "1234-5678"),
            Row(3, "PHONE-1", "Blue 128", "PHONE-128", "12345678")
        };
        var repository = new FakeRepository();

        var result = await Import(rows, repository, false);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error =>
            error.Code == "catalog_import.duplicate_variant_code");
        Assert.Contains(result.Errors, error =>
            error.Code == "catalog_import.duplicate_gtin");
        Assert.Equal(0, repository.FindCalls);
        Assert.Equal(0, repository.WriteCalls);
    }

    [Fact]
    public async Task ExistingGtin_ReturnsConflictWithoutBatchWrite()
    {
        var repository = new FakeRepository
        {
            ExistingGtins = new[] { "12345678" }
        };

        var result = await Import(
            new[] { Row(2, "PHONE-1", "Black 128", "PHONE-128", "1234-5678") },
            repository,
            false);

        Assert.True(result.IsFailure);
        Assert.Equal(CatalogImportErrors.GtinConflictCode,
            Assert.Single(result.Errors).Code);
        Assert.Equal(1, repository.FindCalls);
        Assert.Equal(new[] { "12345678" }, repository.LastGtins);
        Assert.Equal(0, repository.WriteCalls);
    }

    [Fact]
    public async Task ActivateTrue_GroupsRowsAndWritesOneActiveBatch()
    {
        var rows = new[]
        {
            Row(4, "LAPTOP-1", "16/512", "LAPTOP-16-512", "123456789012",
                "Demo Laptop", "Contoso"),
            Row(2, "phone-1", "Black 128", "PHONE-BLACK-128", "12345678"),
            Row(3, "Phone-1", "Blue 256", "PHONE-BLUE-256", "1234567890123")
        };
        var repository = new FakeRepository();
        using var cancellation = new CancellationTokenSource();

        var result = await Import(
            rows, repository, true, cancellation.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.FindCalls);
        Assert.Equal(1, repository.WriteCalls);
        Assert.Equal(cancellation.Token, repository.LastToken);
        Assert.Equal(2, repository.Products.Count);
        Assert.Equal(3, repository.Variants.Count);
        Assert.All(repository.Products, product =>
            Assert.Equal(ProductStatus.Active, product.Status));
        Assert.All(repository.Variants, variant =>
            Assert.Equal(ProductVariantStatus.Active, variant.Status));

        var phone = Assert.Single(repository.Products,
            product => product.Title == "Demo Phone");
        Assert.Equal(2, repository.Variants.Count(
            variant => variant.ProductId == phone.Id));

        var response = result.Value!;
        Assert.Equal(3, response.RowsProcessed);
        Assert.Equal(2, response.ProductsCreated);
        Assert.Equal(3, response.VariantsCreated);
        Assert.True(response.Activated);
        Assert.Equal(new[] { "PHONE-1", "LAPTOP-1" },
            response.Products.Select(product => product.ProductKey));
        Assert.All(response.Products, product =>
        {
            Assert.Equal("Active", product.Status);
            Assert.All(product.Variants, variant =>
                Assert.Equal("Active", variant.Status));
        });
    }

    private static async Task<Result<CatalogImportResponseDto>> Import(
        IReadOnlyCollection<CatalogCsvRow> rows,
        FakeRepository repository,
        bool activate,
        CancellationToken cancellationToken = default)
    {
        var parser = new FakeParser(new(
            rows, Array.Empty<Error>()));
        using var stream = new MemoryStream();
        return await new AdminCatalogImportService(
            parser, repository).ImportAsync(
                stream, activate, cancellationToken);
    }

    private static CatalogCsvRow Row(
        int number,
        string key,
        string variantName,
        string variantCode,
        string? gtin,
        string title = "Demo Phone",
        string brand = "Fabrikam") => new(
            number, key, title, brand,
            "Imported product", variantName, variantCode, gtin);

    private sealed class FakeParser(CatalogCsvParseResult result)
        : ICatalogCsvParser
    {
        public int Calls { get; private set; }

        public Task<CatalogCsvParseResult> ParseAsync(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeRepository : ICatalogBulkRepository
    {
        public IReadOnlyCollection<string> ExistingGtins { get; init; }
            = Array.Empty<string>();
        public int FindCalls { get; private set; }
        public int WriteCalls { get; private set; }
        public IReadOnlyCollection<string> LastGtins { get; private set; }
            = Array.Empty<string>();
        public IReadOnlyCollection<Product> Products { get; private set; }
            = Array.Empty<Product>();
        public IReadOnlyCollection<ProductVariant> Variants { get; private set; }
            = Array.Empty<ProductVariant>();
        public CancellationToken LastToken { get; private set; }

        public Task<IReadOnlyCollection<string>> FindExistingGtinsAsync(
            IReadOnlyCollection<string> normalizedGtins,
            CancellationToken cancellationToken = default)
        {
            FindCalls++;
            LastGtins = normalizedGtins.ToArray();
            LastToken = cancellationToken;
            return Task.FromResult(ExistingGtins);
        }

        public Task<bool> TryCreateBatchAsync(
            IReadOnlyCollection<Product> products,
            IReadOnlyCollection<ProductVariant> variants,
            CancellationToken cancellationToken = default)
        {
            WriteCalls++;
            Products = products.ToArray();
            Variants = variants.ToArray();
            LastToken = cancellationToken;
            return Task.FromResult(true);
        }
    }
}
