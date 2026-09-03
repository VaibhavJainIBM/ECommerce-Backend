using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using ECommerce.Api.BackgroundJobs;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Shopping;
using ECommerce.Application.Storefront;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.ValueObjects;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.IntegrationTests;

// Explicit opt-in: these tests CREATE and DROP their own unique SQL Server database.
// The supplied connection is only used as a server credential; its database is replaced.
public sealed class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ECOMMERCE_TEST_SQLSERVER")))
            Skip = "Set ECOMMERCE_TEST_SQLSERVER to run isolated real-SQL HTTP integration tests.";
    }
}

public sealed class ShoppingApiTests(ShoppingSqlFixture fixture) : IClassFixture<ShoppingSqlFixture>
{
    [SqlFact]
    public async Task Storefront_IsAnonymous_Searchable_AndFiltersAllInactiveStates()
    {
        var family = Guid.NewGuid().ToString("N");
        var live = await fixture.SeedListingAsync([3, 4], family);
        await fixture.SeedListingAsync([10], family, activeSeller: false);
        await fixture.SeedListingAsync([10], family, activeProduct: false);
        await fixture.SeedListingAsync([10], family, activeVariant: false);
        await fixture.SeedListingAsync([10], family, activeListing: false);
        await fixture.SeedListingAsync([10], family, activeWarehouse: false);
        await fixture.SeedListingAsync([0], family);
        using var client = fixture.Client();

        var response = await Read<PagedStorefrontListingsResponseDto>(
            await client.GetAsync($"/api/storefront/listings?search={family}&pageSize=1"));
        Assert.Equal(1, response.TotalCount);
        Assert.Equal(live.ListingId, Assert.Single(response.Items).ListingId);
        Assert.Equal(7L, response.Items.Single().AvailableQuantity);
        var detail = await Read<StorefrontListingResponseDto>(
            await client.GetAsync($"/api/storefront/listings/{live.ListingId}"));
        Assert.Equal(7L, detail.AvailableQuantity);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/storefront/listings?pageSize=101")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/storefront/listings/{Guid.NewGuid()}")).StatusCode);
    }

    [SqlFact]
    public async Task Cart_RequiresValidJwt_EnforcesOwnershipAndQuantity_AndSupportsRemoval()
    {
        var listing = await fixture.SeedListingAsync([10]);
        var first = await fixture.SeedUserAsync();
        var second = await fixture.SeedUserAsync();
        using var anonymous = fixture.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/cart")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/orders")).StatusCode);
        anonymous.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/cart")).StatusCode);
        using var a = fixture.Client(first);
        using var b = fixture.Client(second);

        var cart = await Put(a, listing.ListingId, 2);
        Assert.Equal(2, Assert.Single(cart.Items).Quantity);
        Assert.Empty((await Read<CartResponseDto>(await b.GetAsync("/api/cart"))).Items);
        var updated = await Put(a, listing.ListingId, 3);
        Assert.NotEqual(cart.RowVersion, updated.RowVersion);
        Assert.Equal(3, Assert.Single(updated.Items).Quantity);
        foreach (var quantity in new[] { 0, 100 })
            Assert.Equal(HttpStatusCode.BadRequest,
                (await a.PutAsJsonAsync($"/api/cart/items/{listing.ListingId}", new { quantity })).StatusCode);
        Assert.Empty((await Read<CartResponseDto>(
            await a.DeleteAsync($"/api/cart/items/{listing.ListingId}"))).Items);
        await Put(a, listing.ListingId, 1);
        Assert.Empty((await Read<CartResponseDto>(await a.DeleteAsync("/api/cart"))).Items);
    }

    [SqlFact]
    public async Task Checkout_IsAtomic_SnapshotsPrices_ReplaysIdempotently_AndHidesOtherCustomersOrders()
    {
        var listing = await fixture.SeedListingAsync([10]);
        var customer = await fixture.SeedUserAsync();
        using var client = fixture.Client(customer);
        var cart = await Put(client, listing.ListingId, 2);
        var request = Request(cart);
        var key = Guid.NewGuid();
        var createdResponse = await Checkout(client, request, key);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.NotNull(createdResponse.Headers.Location);
        Assert.Equal("false", createdResponse.Headers.GetValues("Idempotency-Replayed").Single());
        var order = await Read<OrderResponseDto>(createdResponse);
        Assert.Equal("PendingPayment", order.Status);
        Assert.Equal(200m, order.TotalAmount);
        Assert.Equal(listing.Title, Assert.Single(order.Items).ProductTitle);
        Assert.Equal(100m, order.Items.Single().UnitPriceAmount);
        Assert.Empty((await Read<CartResponseDto>(await client.GetAsync("/api/cart"))).Items);

        await using (var db = fixture.CreateDb())
        {
            Assert.Equal(2, await db.InventoryItems.Where(x => x.SellerListingId == listing.ListingId)
                .SumAsync(x => x.ReservedQuantity));
            Assert.Equal(1, await db.Orders.CountAsync(x => x.CustomerId == customer));
            var live = await db.SellerListings.SingleAsync(x => x.Id == listing.ListingId);
            live.ChangePrice(new Money(150, "INR"));
            await db.SaveChangesAsync();
        }
        var replayResponse = await Checkout(client, request, key);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.Equal("true", replayResponse.Headers.GetValues("Idempotency-Replayed").Single());
        var replay = await Read<OrderResponseDto>(replayResponse);
        Assert.Equal(order.OrderId, replay.OrderId);
        Assert.Equal(200m, replay.TotalAmount);
        Assert.Equal(100m, replay.Items.Single().UnitPriceAmount);
        Assert.Equal(HttpStatusCode.Conflict,
            (await Checkout(client, Request(cart, recipient: "Changed Recipient"), key)).StatusCode);

        using var other = fixture.Client(await fixture.SeedUserAsync());
        Assert.Equal(HttpStatusCode.NotFound,
            (await other.GetAsync($"/api/orders/{order.OrderId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await other.PostAsync($"/api/orders/{order.OrderId}/cancel", null)).StatusCode);
        var history = await Read<PagedOrdersResponseDto>(await client.GetAsync("/api/orders"));
        Assert.Equal(order.OrderId, Assert.Single(history.Items).OrderId);
        await using var check = fixture.CreateDb();
        Assert.Equal(2, await check.InventoryItems.Where(x => x.SellerListingId == listing.ListingId)
            .SumAsync(x => x.ReservedQuantity));
        Assert.Equal(1, await check.Orders.CountAsync(x => x.CustomerId == customer));
    }

    [SqlFact]
    public async Task Checkout_RejectsStaleCartWithoutSavingOrderOrReservingStock()
    {
        var listing = await fixture.SeedListingAsync([10]);
        var customer = await fixture.SeedUserAsync();
        using var client = fixture.Client(customer);
        var old = await Put(client, listing.ListingId, 1);
        var current = await Put(client, listing.ListingId, 2);
        Assert.Equal(HttpStatusCode.Conflict,
            (await Checkout(client, Request(old), Guid.NewGuid())).StatusCode);
        await AssertUnchanged(customer, listing.ListingId, current, client);
    }

    [SqlFact]
    public async Task Checkout_RejectsChangedPriceAndInsufficientStock_WithoutPartialWrites()
    {
        var listing = await fixture.SeedListingAsync([10]);
        var customer = await fixture.SeedUserAsync();
        using var client = fixture.Client(customer);
        var cart = await Put(client, listing.ListingId, 2);
        await using (var db = fixture.CreateDb())
        {
            (await db.SellerListings.SingleAsync(x => x.Id == listing.ListingId))
                .ChangePrice(new Money(125, "INR"));
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict,
            (await Checkout(client, Request(cart), Guid.NewGuid())).StatusCode);
        await AssertUnchanged(customer, listing.ListingId, cart, client);
        var fresh = await Read<CartResponseDto>(await client.GetAsync("/api/cart"));
        await using (var db = fixture.CreateDb())
        {
            (await db.InventoryItems.SingleAsync(x => x.SellerListingId == listing.ListingId)).AdjustOnHand(1);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict,
            (await Checkout(client, Request(fresh), Guid.NewGuid())).StatusCode);
        await AssertUnchanged(customer, listing.ListingId, fresh, client);
    }

    [SqlFact]
    public async Task CancelTwice_ReleasesInventoryExactlyOnce()
    {
        var listing = await fixture.SeedListingAsync([5]);
        using var client = fixture.Client(await fixture.SeedUserAsync());
        var cart = await Put(client, listing.ListingId, 3);
        var order = await Read<OrderResponseDto>(await Checkout(client, Request(cart), Guid.NewGuid()));
        for (var attempt = 0; attempt < 2; attempt++)
            Assert.Equal("Cancelled", (await Read<OrderResponseDto>(
                await client.PostAsync($"/api/orders/{order.OrderId}/cancel", null))).Status);
        await using var db = fixture.CreateDb();
        var stock = await db.InventoryItems.SingleAsync(x => x.SellerListingId == listing.ListingId);
        Assert.Equal(5, stock.OnHandQuantity);
        Assert.Equal(0, stock.ReservedQuantity);
    }

    [SqlFact]
    public async Task MultiVendorCheckout_AllocatesAcrossWarehouses_AndSellerSeesOnlyOwnLines()
    {
        var first = await fixture.SeedListingAsync([2, 3]);
        var second = await fixture.SeedListingAsync([4]);
        var customer = await fixture.SeedUserAsync();
        using var client = fixture.Client(customer);
        await Put(client, first.ListingId, 4);
        var cart = await Put(client, second.ListingId, 1);
        var order = await Read<OrderResponseDto>(await Checkout(client, Request(cart), Guid.NewGuid()));
        Assert.Equal(2, order.Items.Count);
        Assert.Equal(500m, order.TotalAmount);
        await using (var db = fixture.CreateDb())
        {
            var stored = await db.Orders.Include(x => x.Items).ThenInclude(x => x.Allocations)
                .SingleAsync(x => x.Id == order.OrderId);
            var firstLine = stored.Items.Single(x => x.SellerListingId == first.ListingId);
            Assert.Equal(2, firstLine.Allocations.Count);
            Assert.Equal(4, firstLine.Allocations.Sum(x => x.Quantity));
        }
        var owner = await fixture.SeedOwnerAsync(first.SellerId);
        using var seller = fixture.Client(owner);
        var sellerOrders = await Read<PagedSellerOrdersResponseDto>(
            await seller.GetAsync($"/api/sellers/{first.SellerId}/orders"));
        var view = Assert.Single(sellerOrders.Items);
        Assert.Equal(order.OrderId, view.OrderId);
        Assert.Equal(400m, view.SellerSubtotal);
        Assert.Equal(first.SellerId, Assert.Single(view.Items).SellerId);
        Assert.Equal(HttpStatusCode.NotFound,
            (await seller.GetAsync($"/api/sellers/{second.SellerId}/orders")).StatusCode);
        await using (var db = fixture.CreateDb())
        {
            (await db.Users.SingleAsync(x => x.Id == owner)).IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.NotFound,
            (await seller.GetAsync($"/api/sellers/{first.SellerId}/orders")).StatusCode);
    }

    [SqlFact]
    public async Task ConcurrentCheckouts_CannotOversellTheLastUnit()
    {
        var listing = await fixture.SeedListingAsync([1]);
        var first = await fixture.SeedUserAsync();
        var second = await fixture.SeedUserAsync();
        using var a = fixture.Client(first);
        using var b = fixture.Client(second);
        var cartA = await Put(a, listing.ListingId, 1);
        var cartB = await Put(b, listing.ListingId, 1);
        var responses = await Task.WhenAll(
            Checkout(a, Request(cartA), Guid.NewGuid()),
            Checkout(b, Request(cartB), Guid.NewGuid()));
        var diagnostics = string.Join(" | ", await Task.WhenAll(responses.Select(async response =>
            $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}")));
        Assert.True(responses.Any(x => x.StatusCode == HttpStatusCode.Created), diagnostics);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
        await using var db = fixture.CreateDb();
        var stock = await db.InventoryItems.SingleAsync(x => x.SellerListingId == listing.ListingId);
        Assert.Equal(1, stock.OnHandQuantity);
        Assert.Equal(1, stock.ReservedQuantity);
        Assert.Equal(1, await db.Orders.CountAsync(x => x.CustomerId == first || x.CustomerId == second));
    }

    [SqlFact]
    public async Task Expiration_ReleasesOnlyPendingReservations_AndIsIdempotent()
    {
        var listing = await fixture.SeedListingAsync([5]);
        using var client = fixture.Client(await fixture.SeedUserAsync());
        var cart = await Put(client, listing.ListingId, 2);
        var order = await Read<OrderResponseDto>(await Checkout(client, Request(cart), Guid.NewGuid()));
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IShoppingRepository>();
            await repository.ExpireOrdersAsync(DateTimeOffset.UtcNow.AddMinutes(31), 100);
            Assert.Equal(0, await repository.ExpireOrdersAsync(DateTimeOffset.UtcNow.AddMinutes(31), 100));
        }
        var expired = await Read<OrderResponseDto>(await client.GetAsync($"/api/orders/{order.OrderId}"));
        Assert.Equal("Expired", expired.Status);
        await using var db = fixture.CreateDb();
        var stock = await db.InventoryItems.SingleAsync(x => x.SellerListingId == listing.ListingId);
        Assert.Equal(5, stock.OnHandQuantity);
        Assert.Equal(0, stock.ReservedQuantity);
    }

    private async Task AssertUnchanged(Guid customer, Guid listing, CartResponseDto before, HttpClient client)
    {
        var after = await Read<CartResponseDto>(await client.GetAsync("/api/cart"));
        Assert.Equal(before.RowVersion, after.RowVersion);
        Assert.Equal(before.Items.Single().Quantity, after.Items.Single().Quantity);
        await using var db = fixture.CreateDb();
        Assert.False(await db.Orders.AnyAsync(x => x.CustomerId == customer));
        Assert.Equal(0, await db.InventoryItems.Where(x => x.SellerListingId == listing).SumAsync(x => x.ReservedQuantity));
    }

    private static Task<CartResponseDto> Put(HttpClient client, Guid listing, int quantity) =>
        ReadResponse<CartResponseDto>(client.PutAsJsonAsync($"/api/cart/items/{listing}", new { quantity }));

    private static CheckoutRequestDto Request(CartResponseDto cart, string recipient = "Test Customer") => new()
    {
        CartRowVersion = cart.RowVersion,
        ExpectedTotalAmount = cart.TotalAmount,
        CurrencyCode = cart.CurrencyCode,
        RecipientName = recipient,
        Phone = "+919999999999",
        ShippingAddress = new ShippingAddressDto("1 Test Street", "Delhi", "Delhi", "110001", "IN")
    };

    private static Task<HttpResponseMessage> Checkout(HttpClient client, CheckoutRequestDto body, Guid key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders/checkout")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("Idempotency-Key", key.ToString());
        return client.SendAsync(request);
    }

    private static async Task<T> ReadResponse<T>(Task<HttpResponseMessage> response) => await Read<T>(await response);

    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode,
            $"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}

public sealed class ShoppingSqlFixture : IAsyncLifetime
{
    private const string DatabasePrefix = "ECommerceMvpTests_";
    private readonly string databaseName = DatabasePrefix + Guid.NewGuid().ToString("N");
    private readonly byte[] signingKey = RandomNumberGenerator.GetBytes(32);
    private string? connectionString;
    public ShoppingTestFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var source = Environment.GetEnvironmentVariable("ECOMMERCE_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(source)) return;
        var builder = new SqlConnectionStringBuilder(source) { InitialCatalog = databaseName, Pooling = false };
        connectionString = builder.ConnectionString;
        await using (var db = CreateDb()) await db.Database.MigrateAsync();
        Factory = new ShoppingTestFactory(connectionString, signingKey);
        using var warmup = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
    }

    public ECommerceDbContext CreateDb()
    {
        if (connectionString is null) throw new InvalidOperationException("SQL integration tests are not enabled.");
        return new ECommerceDbContext(new DbContextOptionsBuilder<ECommerceDbContext>()
            .UseSqlServer(connectionString).Options);
    }

    public HttpClient Client(Guid? user = null)
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        if (user.HasValue)
        {
            var token = new JwtSecurityToken("ecommerce-integration", "ecommerce-integration",
                [new Claim(JwtRegisteredClaimNames.Sub, user.Value.ToString())],
                notBefore: DateTime.UtcNow.AddMinutes(-1), expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(signingKey), SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                new JwtSecurityTokenHandler().WriteToken(token));
        }
        return client;
    }

    public async Task<Guid> SeedUserAsync()
    {
        await using var db = CreateDb();
        var email = Guid.NewGuid().ToString("N") + "@integration.invalid";
        var user = new ApplicationUser
        {
            FirstName = "Integration", LastName = "Customer", Email = email,
            UserName = email, NormalizedEmail = email.ToUpperInvariant(), NormalizedUserName = email.ToUpperInvariant()
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    public async Task<Guid> SeedOwnerAsync(Guid sellerId)
    {
        var user = await SeedUserAsync();
        await using var db = CreateDb();
        var member = new SellerMember(sellerId, user);
        member.Activate();
        var role = new SellerRole(sellerId, "Owner", isBuiltIn: true);
        db.Add(new SellerMemberRole(member, role));
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<TestListing> SeedListingAsync(int[] quantities, string? family = null,
        bool activeSeller = true, bool activeProduct = true, bool activeVariant = true,
        bool activeListing = true, bool activeWarehouse = true)
    {
        await using var db = CreateDb();
        var tag = Guid.NewGuid().ToString("N");
        var seller = new Seller("Test Seller " + tag, "Test Legal " + tag);
        if (activeSeller) { seller.SubmitForReview(); seller.Approve(); }
        var product = new Product("Mvp " + (family ?? tag), "TestBrand", "Original product description");
        if (activeProduct) product.Activate();
        var variant = new ProductVariant(product.Id, "Black / 128", "VAR-" + tag);
        if (activeVariant) variant.Activate();
        var listing = new SellerListing(seller.Id, variant.Id, "SKU-" + tag, new Money(100m, "INR"));
        if (activeListing) { listing.SubmitForReview(); listing.Approve(); }
        db.AddRange(seller, product, variant, listing);
        for (var index = 0; index < quantities.Length; index++)
        {
            var warehouse = new Warehouse(seller.Id, "Warehouse " + index, "WH-" + index,
                new Address("1 Warehouse Street", "Delhi", "Delhi", "110001", "IN"));
            if (activeWarehouse) warehouse.Activate();
            var inventory = new InventoryItem(warehouse, listing);
            if (quantities[index] > 0) inventory.Receive(quantities[index]);
            db.InventoryItems.Add(inventory);
        }
        await db.SaveChangesAsync();
        return new TestListing(listing.Id, seller.Id, product.Id, product.Title);
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null) await Factory.DisposeAsync();
        if (connectionString is null) return;
        // Never delete a configured application database: only this fixture's exact unique database.
        var target = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (target != databaseName || !target.StartsWith(DatabasePrefix, StringComparison.Ordinal) ||
            !Guid.TryParseExact(target[DatabasePrefix.Length..], "N", out _))
            throw new InvalidOperationException("Refusing to delete an unexpected SQL database.");
        await using var db = CreateDb();
        await db.Database.EnsureDeletedAsync();
    }
}

public sealed record TestListing(Guid ListingId, Guid SellerId, Guid ProductId, string Title);

public sealed class ShoppingTestFactory(string connectionString, byte[] signingKey) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["AdminSeed:Enabled"] = "false",
                ["Jwt:Issuer"] = "ecommerce-integration",
                ["Jwt:Audience"] = "ecommerce-integration",
                ["Jwt:SigningKey"] = Convert.ToBase64String(signingKey),
                ["Jwt:AccessTokenMinutes"] = "30",
                ["Logging:LogLevel:Default"] = "Warning"
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ECommerceDbContext>();
            services.RemoveAll<DbContextOptions<ECommerceDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ECommerceDbContext>>();
            services.AddDbContext<ECommerceDbContext>(options => options.UseSqlServer(connectionString));
            foreach (var descriptor in services.Where(x => x.ServiceType == typeof(IHostedService) &&
                         x.ImplementationType == typeof(OrderExpirationWorker)).ToArray())
                services.Remove(descriptor);
        });
    }
}
