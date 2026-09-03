# ECommerce storefront, cart and orders demo

This checkpoint implements shopping without payments. Checkout creates a **PendingPayment** order and reserves inventory for 30 minutes. It does not charge a card, confirm payment, or promise shipment. Payment integration is the next phase.

## Run

Open a terminal at `C:\Users\VaibhavJain\Desktop\Week 1\C#\Capstone 1\ECommerce`.

Stop the previous API with Ctrl+C before rebuilding. Keep secrets in the API project's .NET User Secrets; do not paste tokens/passwords into this document or commit them.

```powershell
dotnet build '.\ECommerce.sln'
dotnet ef database update --project '.\src\ECommerce.Infrastructure\ECommerce.Infrastructure.csproj' --startup-project '.\src\ECommerce.Api\ECommerce.Api.csproj'
dotnet run --project '.\src\ECommerce.Api\ECommerce.Api.csproj' --launch-profile https
```

The additive migration creates cart/order/reservation tables; it does not replace existing catalog/inventory data. Use a local development database. Confirm the configured connection is the intended ECommerceDb before applying it.

## Postman setup

Import `ECommerce-MVP.postman_collection.json` from this directory. Set collection variables locally:

- `baseUrl`: the HTTPS URL printed by the API (default `https://localhost:7042`).
- `customerToken`: access token from an ordinary user's existing login endpoint.
- `sellerOwnerToken`: the Owner's token for the seller being demonstrated.
- `listingId` and `sellerId`: the first storefront search sets these automatically; choose your desired listing if needed.
- `quantity`: defaults to 1 and must be 1..99.

Do not export the collection with real tokens. Prefer Postman environment variables/Vault for credentials. A PlatformAdmin token does not automatically grant seller Owner membership.

The demo assumes the seller, product, variant, listing and warehouse are Active, with available stock. An empty storefront is not an API failure; verify those conditions. Creating a cart never reserves stock.

## Demonstration order

1. **Browse storefront**: anonymous GET `/api/storefront/listings`. Search and pagination are supported. It selects a purchasable listing.
2. **Read public listing**: anonymous GET `/api/storefront/listings/{listingId}`.
3. **Set cart item**: authenticated PUT `/api/cart/items/{listingId}` with `{ "quantity": 1 }`. PUT sets the quantity; it does not add to the previous quantity. The script saves the latest cart rowVersion/total/currency and creates a fresh checkout key and frozen request body.
4. **Read cart**: authenticated GET `/api/cart`. The server calculates current prices and flags unavailable items. A changed cart or price will be rejected at checkout until reviewed.
5. **Checkout**: POST `/api/orders/checkout`, sending `Idempotency-Key: <GUID>` and the frozen request. Expect 201, a Location header, and PendingPayment. Order prices and address are immutable snapshots. The cart is cleared and stock is reserved atomically.
6. **Retry exact checkout**: repeat the same key and body. Expect 200 and the same orderId, without reserving stock twice. Never generate a new key for a network retry. Same key with a changed body returns 409.
7. **My orders / one order**: GET `/api/orders?page=1&pageSize=20` and GET `/api/orders/{orderId}`. Another customer's token must not be able to read this order.
8. **Seller orders**: GET `/api/sellers/{sellerId}/orders` using the seller Owner token. Only that seller's lines and subtotal are included, not other sellers' lines.
9. **Cancel order**: POST `/api/orders/{orderId}/cancel`, no body. An unpaid pending order is cancelled and its reserved stock is released atomically. Repeating cancellation must not release it twice.
10. **Verify storefront and cart**: availability is restored after cancellation; checkout left the cart empty. Add an item again to begin a different checkout with a new key.

Unpaid pending orders expire after 30 minutes; the background cleanup releases reservations in batches. Expiration is not instantaneous at the exact timestamp: allow for the cleanup interval. A paused API cannot run cleanup, so start it again to resume cleanup.

## Checkout body

Use the **cart's** rowVersion, not a listing or inventory rowVersion. Never let the frontend submit a customerId or authoritative unit price.

```json
{
  "cartRowVersion": "COPY_CART_ROW_VERSION",
  "expectedTotalAmount": 79999.00,
  "currencyCode": "INR",
  "recipientName": "Demo Customer",
  "phone": "9999999999",
  "shippingAddress": {
    "line1": "1 Main Road",
    "line2": null,
    "city": "Delhi",
    "stateOrProvince": "Delhi",
    "postalCode": "110001",
    "countryCode": "IN"
  }
}
```

ExpectedTotalAmount and CurrencyCode confirm what the customer reviewed; the server independently calculates the authoritative amount and rejects mismatches. If stock, cart version or price changed, expect 409: reload the cart, review it, and start a new logical checkout with a new key. An idempotent replay must keep the original body even though successful checkout cleared the cart.

## Additional endpoints and checks

- DELETE `/api/cart/items/{listingId}` removes one line.
- DELETE `/api/cart` clears all lines; this does not cancel an existing order.
- Anonymous cart/order requests return 401.
- Quantity 0 or 100 returns 400.
- Missing/invalid checkout key or malformed cart rowVersion returns 400.
- A stale cart version, changed price, insufficient stock or reused key with changed body returns 409.
- Other-customer order access returns 404.
- Seller access is checked by the seller Owner policy, not just by possession of any JWT.
- Page must be positive and pageSize must be 1..100.

## Known MVP boundaries

No real or fake payment-success endpoint is provided. No payment, refund, shipping fee, tax calculation, delivery quote, promotion, pack/ship/deliver workflow or email is claimed by this checkpoint. The displayed total is the merchandise total. Mixed-currency carts cannot check out. Stock is allocated across active warehouses and must be protected by the database transaction/concurrency checks. The seller view is for viewing orders, not marking them paid or shipped.

## Automated checks

```powershell
dotnet test '.\tests\ECommerce.UnitTests\ECommerce.UnitTests.csproj'
dotnet test '.\tests\ECommerce.IntegrationTests\ECommerce.IntegrationTests.csproj'
```

Unit tests do not require SQL Server. Database/API integration checks may require a configured local test database; read their output and configuration before running against non-test data.
