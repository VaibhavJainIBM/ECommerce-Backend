using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.UnitTests;

public sealed class ShoppingDomainTests
{
    [Fact]
    public void Cart_requires_customer_identity()
    {
        Assert.Throws<ArgumentException>(() => new Cart(Guid.Empty));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(100)]
    public void Cart_rejects_invalid_quantities_without_mutation(int quantity)
    {
        var cart = new Cart(Guid.NewGuid());
        Assert.Throws<ArgumentOutOfRangeException>(() => cart.SetItem(Guid.NewGuid(), quantity));
        Assert.Empty(cart.Items);
        Assert.Null(cart.UpdatedAtUtc);
    }

    [Fact]
    public void Cart_set_replaces_quantity_instead_of_adding_duplicate_lines()
    {
        var cart = new Cart(Guid.NewGuid());
        var listingId = Guid.NewGuid();
        cart.SetItem(listingId, 2);
        var firstItemId = Assert.Single(cart.Items).Id;
        cart.SetItem(listingId, 4);
        var item = Assert.Single(cart.Items);
        Assert.Equal(firstItemId, item.Id);
        Assert.Equal(cart.Id, item.CartId);
        Assert.Equal(4, item.Quantity);
        Assert.NotNull(cart.UpdatedAtUtc);
    }

    [Fact]
    public void Cart_limits_distinct_lines_but_allows_update_at_limit()
    {
        var cart = new Cart(Guid.NewGuid());
        for (var i = 0; i < Cart.MaximumLines; i++)
            cart.SetItem(Guid.NewGuid(), 1);
        Assert.Throws<InvalidOperationException>(() => cart.SetItem(Guid.NewGuid(), 1));
        cart.SetItem(cart.Items.First().SellerListingId, 99);
        Assert.Equal(Cart.MaximumLines, cart.Items.Count);
        Assert.Equal(99, cart.Items.First().Quantity);
    }

    [Fact]
    public void Cart_remove_and_clear_are_idempotent()
    {
        var cart = new Cart(Guid.NewGuid());
        var listingId = Guid.NewGuid();
        cart.SetItem(listingId, 1);
        cart.RemoveItem(listingId);
        cart.RemoveItem(listingId);
        Assert.Empty(cart.Items);
        cart.SetItem(Guid.NewGuid(), 1);
        cart.Clear();
        cart.Clear();
        Assert.Empty(cart.Items);
        Assert.NotNull(cart.UpdatedAtUtc);
    }

    [Fact]
    public void Cart_rejects_empty_listing_id()
    {
        var cart = new Cart(Guid.NewGuid());
        Assert.Throws<ArgumentException>(() => cart.SetItem(Guid.Empty, 1));
        Assert.Throws<ArgumentException>(() => cart.RemoveItem(Guid.Empty));
    }

    [Fact]
    public void Order_totals_use_server_snapshot_prices_and_normalized_currency()
    {
        var order = NewOrder();
        var first = NewLine(price: 125.50m, quantity: 2, currency: "inr");
        var second = NewLine(price: 20m, quantity: 3);
        order.AddItem(first);
        order.AddItem(second);
        Assert.Equal(311m, order.TotalAmount);
        Assert.Equal("INR", order.CurrencyCode);
        Assert.Equal(251m, first.LineTotal);
        Assert.Equal(order.Id, first.OrderId);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal($"ORD-{order.Id:N}", order.OrderNumber);
    }

    [Fact]
    public void Order_rejects_mixed_currencies_without_changing_total()
    {
        var order = NewOrder();
        order.AddItem(NewLine(price: 100m));
        var incompatible = NewLine(currency: "USD");
        Assert.Throws<InvalidOperationException>(() => order.AddItem(incompatible));
        Assert.Equal(100m, order.TotalAmount);
        Assert.Single(order.Items);
        Assert.Equal(Guid.Empty, incompatible.OrderId);
    }

    [Fact]
    public void Order_rejects_duplicate_listing_lines()
    {
        var order = NewOrder();
        var listingId = Guid.NewGuid();
        order.AddItem(NewLine(listingId: listingId));
        Assert.Throws<InvalidOperationException>(() => order.AddItem(NewLine(listingId: listingId)));
        Assert.Single(order.Items);
    }

    [Fact]
    public void Order_rejects_a_line_already_attached_to_another_order()
    {
        var original = NewOrder();
        var other = NewOrder();
        var line = NewLine();
        original.AddItem(line);
        Assert.Throws<InvalidOperationException>(() => other.AddItem(line));
        Assert.Empty(other.Items);
        Assert.Equal(0m, other.TotalAmount);
    }

    [Fact]
    public void Order_total_cannot_exceed_SQL_decimal_capacity()
    {
        var order = NewOrder();
        order.AddItem(NewLine(price: 6_000_000_000_000_000m));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            order.AddItem(NewLine(price: 6_000_000_000_000_000m)));
        Assert.Equal(6_000_000_000_000_000m, order.TotalAmount);
        Assert.Single(order.Items);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(100)]
    public void Order_line_rejects_invalid_quantity(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewLine(quantity: quantity));
    }

    [Fact]
    public void Order_line_rejects_fractional_minor_units()
    {
        Assert.Throws<ArgumentException>(() => NewLine(price: 10.001m));
    }

    [Fact]
    public void Order_line_rejects_zero_price()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewLine(price: 0m));
    }

    [Fact]
    public void Order_line_total_cannot_exceed_SQL_decimal_capacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NewLine(price: 6_000_000_000_000_000m, quantity: 2));
    }

    [Fact]
    public void Order_item_preserves_snapshot_when_listing_price_changes()
    {
        var sellerId = Guid.NewGuid();
        var listing = new SellerListing(sellerId, Guid.NewGuid(), "SKU-1", new Money(100m, "INR"));
        var line = NewLine(sellerId: sellerId, listingId: listing.Id, price: listing.Price.Amount);
        listing.ChangePrice(new Money(120m, "INR"));
        Assert.Equal(100m, line.UnitPriceAmount);
        Assert.Equal(100m, line.LineTotal);
    }

    [Fact]
    public void Order_cancel_is_idempotent_and_closes_mutation()
    {
        var order = NewOrder();
        order.AddItem(NewLine());
        Assert.True(order.Cancel());
        Assert.False(order.Cancel());
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Throws<InvalidOperationException>(() => order.AddItem(NewLine()));
        Assert.False(order.Expire(DateTimeOffset.UtcNow.AddDays(1)));
    }

    [Fact]
    public void Order_expires_only_when_due_and_cannot_expire_twice()
    {
        var due = DateTimeOffset.UtcNow.AddMinutes(15);
        var order = NewOrder(due);
        Assert.False(order.Expire(due.AddTicks(-1)));
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.True(order.Expire(due));
        Assert.Equal(OrderStatus.Expired, order.Status);
        Assert.False(order.Expire(due.AddMinutes(1)));
        Assert.False(order.Cancel());
        Assert.Throws<InvalidOperationException>(() => order.AddItem(NewLine()));
    }

    [Fact]
    public void Allocation_records_warehouse_inventory_without_reserving_it()
    {
        var sellerId = Guid.NewGuid();
        var listing = new SellerListing(sellerId, Guid.NewGuid(), "SKU-1", new Money(100m, "INR"));
        var first = NewInventory(listing);
        var second = NewInventory(listing);
        var line = NewLine(sellerId: sellerId, listingId: listing.Id, quantity: 5);
        line.Allocate(first, 2);
        line.Allocate(second, 3);
        Assert.Equal(5, line.Allocations.Sum(x => x.Quantity));
        Assert.Equal(0, first.ReservedQuantity);
        Assert.All(line.Allocations, x => Assert.Equal(line.Id, x.OrderItemId));
    }

    [Fact]
    public void Allocation_rejects_inventory_from_another_seller_or_listing()
    {
        var line = NewLine();
        var otherListing = new SellerListing(Guid.NewGuid(), Guid.NewGuid(), "OTHER", new Money(1m, "INR"));
        Assert.Throws<InvalidOperationException>(() => line.Allocate(NewInventory(otherListing), 1));
        Assert.Empty(line.Allocations);
    }

    [Fact]
    public void Allocation_cannot_exceed_line_quantity_or_duplicate_inventory()
    {
        var sellerId = Guid.NewGuid();
        var listing = new SellerListing(sellerId, Guid.NewGuid(), "SKU-1", new Money(100m, "INR"));
        var inventory = NewInventory(listing);
        var line = NewLine(sellerId: sellerId, listingId: listing.Id, quantity: 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => line.Allocate(inventory, 0));
        Assert.Throws<InvalidOperationException>(() => line.Allocate(inventory, 3));
        line.Allocate(inventory, 1);
        Assert.Throws<InvalidOperationException>(() => line.Allocate(inventory, 1));
        Assert.Single(line.Allocations);
    }

    [Fact]
    public void Order_validates_checkout_identity_and_hash()
    {
        Assert.Throws<ArgumentException>(() => new Order(Guid.NewGuid(), Guid.Empty,
            new string('A', 64), "Customer", "9999999999", Address(), DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => new Order(Guid.NewGuid(), Guid.NewGuid(),
            "invalid", "Customer", "9999999999", Address(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public Order_accepts_32_character_phone_and_rejects_33()
    {
        var accepted = new Order(Guid.NewGuid(), Guid.NewGuid(), new string('A', 64),
            "Customer", new string('1', 32), Address(), DateTimeOffset.UtcNow);
        Assert.Equal(32, accepted.Phone.Length);
        Assert.Throws<ArgumentException>(() => new Order(Guid.NewGuid(), Guid.NewGuid(),
            new string('A', 64), "Customer", new string('1', 33), Address(), DateTimeOffset.UtcNow));
    }

    private static Order NewOrder(DateTimeOffset? expiresAt = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), "Customer", "9999999999",
            Address(), expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(15));

    private static Address Address() => new("123 Main Road", "Delhi", "Delhi", "110001", "IN");

    private static OrderItem NewLine(decimal price = 100m, int quantity = 1,
        string currency = "INR", Guid? sellerId = null, Guid? listingId = null) =>
        new(sellerId ?? Guid.NewGuid(), listingId ?? Guid.NewGuid(), Guid.NewGuid(),
            "Electronics Seller", "Phone", "Black / 128GB", "SKU-1", price, currency, quantity);

    private static InventoryItem NewInventory(SellerListing listing)
    {
        var warehouse = new Warehouse(listing.SellerId, "Main", "MAIN", Address());
        var item = new InventoryItem(warehouse, listing);
        item.Receive(20);
        return item;
    }
}
