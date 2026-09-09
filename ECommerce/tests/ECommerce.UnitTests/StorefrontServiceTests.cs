using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Storefront;

namespace ECommerce.UnitTests;

public sealed class StorefrontServiceTests
{
    [Theory]
    [InlineData(0, 20, "storefront.page_invalid")]
    [InlineData(1, 0, "storefront.page_size_invalid")]
    [InlineData(1, 101, "storefront.page_size_invalid")]
    [InlineData(int.MaxValue, 100, "storefront.pagination_too_deep")]
    public async Task InvalidPagination_DoesNotQueryRepository(
        int page, int pageSize, string expectedCode)
    {
        var repository = new FakeRepository();
        var service = new StorefrontService(repository);

        var result = await service.SearchAsync(new StorefrontQueryDto
        {
            Page = page,
            PageSize = pageSize
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == expectedCode);
        Assert.Equal(0, repository.SearchCalls);
    }

    [Fact]
    public async Task TooLongSearch_DoesNotQueryRepository()
    {
        var repository = new FakeRepository();
        var result = await new StorefrontService(repository).SearchAsync(
            new StorefrontQueryDto { Search = new string('x', 101) });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors,
            error => error.Code == StorefrontErrors.SearchTooLong.Code);
        Assert.Equal(0, repository.SearchCalls);
    }

    [Fact]
    public async Task Search_TrimsQueryAndCalculatesPagination()
    {
        var repository = new FakeRepository { TotalCount = 41 };
        using var cancellation = new CancellationTokenSource();

        var result = await new StorefrontService(repository).SearchAsync(
            new StorefrontQueryDto
            {
                Search = "  phone  ", Page = 2, PageSize = 20
            }, cancellation.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal("phone", repository.LastCriteria!.Search);
        Assert.Equal(20, repository.LastCriteria.Skip);
        Assert.Equal(20, repository.LastCriteria.Take);
        Assert.Equal(cancellation.Token, repository.LastToken);
        Assert.Equal(3, result.Value!.TotalPages);
        Assert.Equal(41, result.Value.TotalCount);
    }

    [Fact]
    public async Task NullQuery_UsesDefaultsAndEmptyResultHasZeroPages()
    {
        var repository = new FakeRepository();
        var result = await new StorefrontService(repository).SearchAsync(null);

        Assert.True(result.IsSuccess);
        Assert.Null(repository.LastCriteria!.Search);
        Assert.Equal(0, repository.LastCriteria.Skip);
        Assert.Equal(20, repository.LastCriteria.Take);
        Assert.Equal(0, result.Value!.TotalPages);
    }

    [Fact]
    public async Task Filters_AreNormalizedAndPassedToRepository()
    {
        var repository = new FakeRepository();

        var result = await new StorefrontService(repository).SearchAsync(
            new StorefrontQueryDto
            {
                Brand = "  DemoTech  ",
                MinPrice = 100,
                MaxPrice = 500,
                Sort = StorefrontSortNames.PriceDescending
            });

        Assert.True(result.IsSuccess);
        Assert.Equal("DemoTech", repository.LastCriteria!.Brand);
        Assert.Equal(100, repository.LastCriteria.MinPrice);
        Assert.Equal(500, repository.LastCriteria.MaxPrice);
        Assert.Equal(StorefrontSort.PriceDescending,
            repository.LastCriteria.Sort);
    }

    [Fact]
    public async Task InvalidFilters_DoNotQueryRepository()
    {
        var repository = new FakeRepository();

        var result = await new StorefrontService(repository).SearchAsync(
            new StorefrontQueryDto
            {
                Brand = new string('b', 151),
                MinPrice = 20,
                MaxPrice = 10,
                Sort = "popular"
            });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors,
            error => error.Code == StorefrontErrors.BrandTooLong.Code);
        Assert.Contains(result.Errors,
            error => error.Code == StorefrontErrors.PriceRangeInvalid.Code);
        Assert.Contains(result.Errors,
            error => error.Code == StorefrontErrors.SortInvalid.Code);
        Assert.Equal(0, repository.SearchCalls);
    }

    [Fact]
    public async Task EmptyListingId_DoesNotQueryRepository()
    {
        var repository = new FakeRepository();
        var result = await new StorefrontService(repository).GetByIdAsync(Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(StorefrontErrors.ListingNotFoundCode,
            Assert.Single(result.Errors).Code);
        Assert.Equal(0, repository.FindCalls);
    }

    [Fact]
    public async Task UnknownListing_ReturnsNotFound()
    {
        var repository = new FakeRepository();
        var result = await new StorefrontService(repository).GetByIdAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(StorefrontErrors.ListingNotFoundCode,
            Assert.Single(result.Errors).Code);
        Assert.Equal(1, repository.FindCalls);
    }

    private sealed class FakeRepository : IStorefrontRepository
    {
        public int TotalCount { get; init; }
        public int SearchCalls { get; private set; }
        public int FindCalls { get; private set; }
        public StorefrontSearchCriteria? LastCriteria { get; private set; }
        public CancellationToken LastToken { get; private set; }

        public Task<StorefrontListingPage> SearchAsync(
            StorefrontSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            SearchCalls++;
            LastCriteria = criteria;
            LastToken = cancellationToken;
            return Task.FromResult(new StorefrontListingPage(
                Array.Empty<StorefrontListingReadModel>(), TotalCount));
        }

        public Task<StorefrontListingReadModel?> FindByIdAsync(
            Guid listingId, CancellationToken cancellationToken = default)
        {
            FindCalls++;
            return Task.FromResult<StorefrontListingReadModel?>(null);
        }
    }
}
