using ECommerce.Application.Abstractions.Authentication;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common;
using ECommerce.Application.Shopping;

namespace ECommerce.UnitTests;

public sealed class ShoppingServiceTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly string Key = Guid.NewGuid().ToString();

    [Fact]
    public async Task AnonymousUserCannotReadOrMutateShoppingData()
    {
        var repository = new FakeRepository();
        var service = new ShoppingService(repository, new FakeUser(null));
        AssertUnauthenticated(await service.GetCartAsync());
        AssertUnauthenticated(await service.SetCartItemAsync(Guid.NewGuid(), new() { Quantity = 1 }));
        AssertUnauthenticated(await service.RemoveCartItemAsync(Guid.NewGuid()));
        AssertUnauthenticated(await service.ClearCartAsync());
        AssertUnauthenticated(await service.CheckoutAsync(Key, Checkout()));
        AssertUnauthenticated(await service.GetOrdersAsync(null));
        AssertUnauthenticated(await service.GetOrderAsync(Guid.NewGuid()));
        AssertUnauthenticated(await service.CancelOrderAsync(Guid.NewGuid()));
        AssertUnauthenticated(await service.GetSellerOrdersAsync(Guid.NewGuid(), null));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task EmptyIdentityIsUnauthenticated()
    {
        var repository = new FakeRepository();
        var service = new ShoppingService(repository, new FakeUser(Guid.Empty));
        AssertUnauthenticated(await service.GetCartAsync());
        Assert.Equal(0, repository.Calls);
    }

    [Theory]
    [InlineData(-1)] [InlineData(0)] [InlineData(100)] [InlineData(int.MaxValue)]
    public async Task InvalidQuantityNeverReachesRepository(int quantity)
    {
        var (service, repository) = Create();
        AssertInvalid(await service.SetCartItemAsync(Guid.NewGuid(), new() { Quantity = quantity }));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task EmptyIdsAndMissingCartItemBodyAreRejected()
    {
        var (service, repository) = Create();
        AssertInvalid(await service.SetCartItemAsync(Guid.Empty, new() { Quantity = 1 }));
        AssertInvalid(await service.SetCartItemAsync(Guid.NewGuid(), null));
        AssertInvalid(await service.RemoveCartItemAsync(Guid.Empty));
        AssertInvalid(await service.GetOrderAsync(Guid.Empty));
        AssertInvalid(await service.CancelOrderAsync(Guid.Empty));
        AssertInvalid(await service.GetSellerOrdersAsync(Guid.Empty, null));
        Assert.Equal(0, repository.Calls);
    }

    [Theory]
    [InlineData(1)] [InlineData(99)]
    public async Task ValidCartWriteUsesAuthenticatedIdAndCancellation(int quantity)
    {
        var (service, repository) = Create();
        var listingId = Guid.NewGuid();
        using var source = new CancellationTokenSource();
        await service.SetCartItemAsync(listingId, new() { Quantity = quantity }, source.Token);
        Assert.Equal(CustomerId, repository.LastCustomerId);
        Assert.Equal(listingId, repository.LastResourceId);
        Assert.Equal(quantity, repository.LastQuantity);
        Assert.Equal(source.Token, repository.LastCancellationToken);
        Assert.Equal(1, repository.Calls);
    }

    [Theory]
    [InlineData(0, 20)] [InlineData(-1, 20)] [InlineData(1, 0)]
    [InlineData(1, 101)] [InlineData(int.MaxValue, 100)]
    public async Task InvalidPaginationIsRejectedForCustomerAndSeller(int page, int pageSize)
    {
        var (service, repository) = Create();
        var query = new OrderQueryDto { Page = page, PageSize = pageSize };
        AssertInvalid(await service.GetOrdersAsync(query));
        AssertInvalid(await service.GetSellerOrdersAsync(Guid.NewGuid(), query));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task DefaultPaginationAndSellerScopeAreForwarded()
    {
        var (service, repository) = Create();
        var sellerId = Guid.NewGuid();
        await service.GetOrdersAsync(null);
        Assert.Equal(CustomerId, repository.LastCustomerId);
        Assert.Equal(1, repository.LastPage);
        Assert.Equal(20, repository.LastPageSize);
        await service.GetSellerOrdersAsync(sellerId, new() { Page = 2, PageSize = 5 });
        Assert.Equal(sellerId, repository.LastResourceId);
        Assert.Equal(2, repository.LastPage);
        Assert.Equal(5, repository.LastPageSize);
    }

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task InvalidCheckoutKeyIsRejected(string? key)
    {
        var (service, repository) = Create();
        AssertInvalid(await service.CheckoutAsync(key, Checkout()));
        Assert.Equal(0, repository.Calls);
    }

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("bad")]
    [InlineData("AQIDBA==")] [InlineData("AAAAAAAAAAAA")]
    public async Task CheckoutRequiresExactlyEightRowVersionBytes(string? rowVersion)
    {
        var (service, repository) = Create();
        AssertInvalid(await service.CheckoutAsync(Key, Checkout(rowVersion: rowVersion)));
        Assert.Equal(0, repository.Calls);
    }

    [Theory]
    [InlineData("0")] [InlineData("-1")] [InlineData("1.001")]
    [InlineData("10000000000000000")]
    public async Task InvalidExpectedTotalIsRejected(string amount)
    {
        var (service, repository) = Create();
        var value = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);
        AssertInvalid(await service.CheckoutAsync(Key, Checkout(total: value)));
        Assert.Equal(0, repository.Calls);
    }

    [Theory]
    [InlineData(null)] [InlineData("IN")] [InlineData("IN12")] [InlineData("I1R")]
    [InlineData("ınr")]
    public async Task CurrencyMustBeThreeAsciiLetters(string? currency)
    {
        var (service, repository) = Create();
        AssertInvalid(await service.CheckoutAsync(Key, Checkout(currency: currency)));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task InvalidAddressAndRecipientReturnErrorsInsteadOfDomainExceptions()
    {
        var (service, repository) = Create();
        AssertInvalid(await service.CheckoutAsync(Key, null));
        AssertInvalid(await service.CheckoutAsync(Key, new CheckoutRequestDto
        {
            CartRowVersion = "AAAAAAAAAAE=", ExpectedTotalAmount = 100m, CurrencyCode = "INR",
            RecipientName = "Demo Customer", Phone = "1234567890", ShippingAddress = null
        }));
        AssertInvalid(await service.CheckoutAsync(Key, Checkout(name: new string('x', 151))));
        AssertInvalid(await service.CheckoutAsync(Key, Checkout(phone: new string('x', 33))));
        AssertInvalid(await service.CheckoutAsync(Key, Checkout(address: new(
            new string('x', 201), "", new string('x', 101), new string('x', 21), "123", new string('x', 201)))));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task CheckoutNormalizesValuesAndHashesEquivalentPayloadsIdentically()
    {
        var (service, repository) = Create();
        await service.CheckoutAsync(Key, Checkout(total: 100m, currency: " inr ", name: " Demo Customer ",
            phone: " 1234567890 ", address: new(" 1 Main Road ", " Delhi ", " Delhi ", " 110001 ", " in ", " ")));
        var first = Assert.IsType<CheckoutCommand>(repository.LastCheckout);
        await service.CheckoutAsync(Key, Checkout(total: 100.00m, address: new("1 Main Road", "Delhi", "Delhi", "110001", "IN")));
        var second = Assert.IsType<CheckoutCommand>(repository.LastCheckout);
        Assert.Equal(first.RequestHash, second.RequestHash);
        Assert.Equal(CustomerId, second.CustomerId);
        Assert.Equal(Guid.Parse(Key), second.CheckoutKey);
        Assert.Equal("INR", first.CurrencyCode);
        Assert.Equal("Demo Customer", first.RecipientName);
        Assert.Equal("IN", first.ShippingAddress.CountryCode);
        Assert.Null(first.ShippingAddress.Line2);
        Assert.Equal(64, first.RequestHash.Length);
    }

    [Fact]
    public async Task ChangedCheckoutDetailsChangeRequestHash()
    {
        var (service, repository) = Create();
        await service.CheckoutAsync(Key, Checkout());
        var firstHash = repository.LastCheckout!.RequestHash;
        await service.CheckoutAsync(Key, Checkout(total: 101m));
        Assert.NotEqual(firstHash, repository.LastCheckout!.RequestHash);
        await service.CheckoutAsync(Key, Checkout(name: "Other recipient"));
        Assert.NotEqual(firstHash, repository.LastCheckout!.RequestHash);
    }

    [Fact]
    public async Task RemovingClearingReadingAndCancellingUseSignedInCustomerScope()
    {
        var (service, repository) = Create();
        var resourceId = Guid.NewGuid();
        await service.GetCartAsync();
        Assert.Equal(CustomerId, repository.LastCustomerId);
        await service.RemoveCartItemAsync(resourceId);
        Assert.Equal(CustomerId, repository.LastCustomerId);
        await service.ClearCartAsync();
        Assert.Equal(CustomerId, repository.LastCustomerId);
        await service.GetOrderAsync(resourceId);
        Assert.Equal(CustomerId, repository.LastCustomerId);
        await service.CancelOrderAsync(resourceId);
        Assert.Equal(CustomerId, repository.LastCustomerId);
        Assert.Equal(resourceId, repository.LastResourceId);
    }

    private static CheckoutRequestDto Checkout(
        string? rowVersion = "AAAAAAAAAAE=", decimal total = 100m, string? currency = "INR",
        string? name = "Demo Customer", string? phone = "1234567890",
        ShippingAddressDto? address = null) => new()
    {
        CartRowVersion = rowVersion, ExpectedTotalAmount = total, CurrencyCode = currency,
        RecipientName = name, Phone = phone,
        ShippingAddress = address ?? new("1 Main Road", "Delhi", "Delhi", "110001", "IN")
    };

    private static (ShoppingService, FakeRepository) Create()
    {
        var repository = new FakeRepository();
        return (new ShoppingService(repository, new FakeUser(CustomerId)), repository);
    }

    private static void AssertInvalid<T>(Result<T> result)
    {
        Assert.True(result.IsFailure);
        Assert.All(result.Errors, error => Assert.Equal(ShoppingErrors.ValidationCode, error.Code));
    }

    private static void AssertUnauthenticated<T>(Result<T> result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(ShoppingErrors.UnauthenticatedCode, Assert.Single(result.Errors).Code);
    }

    private sealed record FakeUser(Guid? UserId) : ICurrentUser;

    private sealed class FakeRepository : IShoppingRepository
    {
        public int Calls { get; private set; }
        public Guid LastCustomerId { get; private set; }
        public Guid LastResourceId { get; private set; }
        public int LastQuantity { get; private set; }
        public int LastPage { get; private set; }
        public int LastPageSize { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }
        public CheckoutCommand? LastCheckout { get; private set; }

        private Task<Result<T>> Record<T>(Guid customerId, Guid resourceId, CancellationToken cancellationToken)
        {
            Calls++;
            LastCustomerId = customerId;
            LastResourceId = resourceId;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(Result<T>.Failure(ShoppingErrors.NotFound("Fake repository response.")));
        }

        public Task<Result<CartResponseDto>> GetCartAsync(Guid customerId, CancellationToken cancellationToken = default) => Record<CartResponseDto>(customerId, Guid.Empty, cancellationToken);
        public Task<Result<CartResponseDto>> SetCartItemAsync(Guid customerId, Guid listingId, int quantity, CancellationToken cancellationToken = default)
        { LastQuantity = quantity; return Record<CartResponseDto>(customerId, listingId, cancellationToken); }
        public Task<Result<CartResponseDto>> RemoveCartItemAsync(Guid customerId, Guid listingId, CancellationToken cancellationToken = default) => Record<CartResponseDto>(customerId, listingId, cancellationToken);
        public Task<Result<CartResponseDto>> ClearCartAsync(Guid customerId, CancellationToken cancellationToken = default) => Record<CartResponseDto>(customerId, Guid.Empty, cancellationToken);
        public Task<Result<CheckoutResponseDto>> CheckoutAsync(CheckoutCommand command, CancellationToken cancellationToken = default)
        { LastCheckout = command; return Record<CheckoutResponseDto>(command.CustomerId, command.CheckoutKey, cancellationToken); }
        public Task<Result<PagedOrdersResponseDto>> GetOrdersAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default)
        { LastPage = page; LastPageSize = pageSize; return Record<PagedOrdersResponseDto>(customerId, Guid.Empty, cancellationToken); }
        public Task<Result<OrderResponseDto>> GetOrderAsync(Guid customerId, Guid orderId, CancellationToken cancellationToken = default) => Record<OrderResponseDto>(customerId, orderId, cancellationToken);
        public Task<Result<OrderResponseDto>> CancelOrderAsync(Guid customerId, Guid orderId, CancellationToken cancellationToken = default) => Record<OrderResponseDto>(customerId, orderId, cancellationToken);
        public Task<Result<PagedSellerOrdersResponseDto>> GetSellerOrdersAsync(Guid sellerId, int page, int pageSize, CancellationToken cancellationToken = default)
        { LastPage = page; LastPageSize = pageSize; return Record<PagedSellerOrdersResponseDto>(Guid.Empty, sellerId, cancellationToken); }
        public Task<int> ExpireOrdersAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
