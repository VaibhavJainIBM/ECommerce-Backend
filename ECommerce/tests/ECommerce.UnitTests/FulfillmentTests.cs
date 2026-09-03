using ECommerce.Application.Abstractions.Authentication;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common;
using ECommerce.Application.Fulfillment;
using ECommerce.Application.Shopping;
using ECommerce.Domain.Entities;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.UnitTests;

public sealed class FulfillmentTests
{
    [Fact]
    public void Shipment_timestamp_is_UTC_and_repeated_dispatch_keeps_original_time()
    {
        var (line, inventory) = NewLine();
        line.Allocate(inventory, 2);
        var timestamp = new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.FromHours(5.5));
        Assert.True(line.MarkShipped(timestamp));
        Assert.False(line.MarkShipped(timestamp.AddMinutes(1)));
        Assert.Equal(timestamp.ToUniversalTime(), line.ShippedAtUtc);
        Assert.Equal(TimeSpan.Zero, line.ShippedAtUtc!.Value.Offset);
    }

    [Fact]
    public void Shipment_requires_full_inventory_allocation()
    {
        var (line, inventory) = NewLine();
        line.Allocate(inventory, 1);
        Assert.Throws<InvalidOperationException>(() => line.MarkShipped(DateTimeOffset.UtcNow));
        Assert.Null(line.ShippedAtUtc);
    }

    [Fact]
    public void Shipment_requires_a_timestamp()
    {
        var (line, inventory) = NewLine();
        line.Allocate(inventory, 2);
        Assert.Throws<ArgumentException>(() => line.MarkShipped(default));
        Assert.Null(line.ShippedAtUtc);
    }

    [Fact]
    public async Task Service_rejects_missing_identity_before_calling_repository()
    {
        var repository = new FakeRepository();
        var service = new FulfillmentService(repository, new FakeUser(null));
        var result = await service.ShipSellerOrderAsync(Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(ShoppingErrors.UnauthenticatedCode, Assert.Single(result.Errors).Code);
        Assert.Null(repository.ActorId);
    }

    [Fact]
    public async Task Service_rejects_missing_seller_or_order_ids()
    {
        var repository = new FakeRepository();
        var service = new FulfillmentService(repository, new FakeUser(Guid.NewGuid()));
        Assert.True((await service.ShipSellerOrderAsync(Guid.Empty, Guid.NewGuid())).IsFailure);
        Assert.True((await service.ShipSellerOrderAsync(Guid.NewGuid(), Guid.Empty)).IsFailure);
        Assert.Null(repository.ActorId);
    }

    [Fact]
    public async Task Service_gets_actor_from_current_identity_not_from_request()
    {
        var actor = Guid.NewGuid();
        var repository = new FakeRepository();
        var service = new FulfillmentService(repository, new FakeUser(actor));
        await service.ShipSellerOrderAsync(Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(actor, repository.ActorId);
    }

    private static (OrderItem Line, InventoryItem Inventory) NewLine()
    {
        var sellerId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var listing = new SellerListing(sellerId, variantId, "DEMO-SKU", new Money(100, "INR"));
        var warehouse = new Warehouse(sellerId, "Delhi", "DELHI", new Address("Main Road", "Delhi", "Delhi", "110001", "IN"));
        var inventory = new InventoryItem(warehouse, listing);
        inventory.Receive(20);
        return (new OrderItem(sellerId, listing.Id, variantId, "Demo Seller", "Demo Phone", "Black", "DEMO-SKU", 100, "INR", 2), inventory);
    }

    private sealed record FakeUser(Guid? UserId) : ICurrentUser;

    private sealed class FakeRepository : IFulfillmentRepository
    {
        public Guid? ActorId { get; private set; }
        public Task<Result<SellerOrderResponseDto>> ShipSellerOrderAsync(
            Guid actorId, Guid sellerId, Guid orderId, CancellationToken cancellationToken = default)
        {
            ActorId = actorId;
            return Task.FromResult(Result<SellerOrderResponseDto>.Failure(ShoppingErrors.NotFound("Test order.")));
        }
    }
}
