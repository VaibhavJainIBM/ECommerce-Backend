# ECommerce MVP: all APIs and Postman bodies

This reference describes the implemented backend. Keep your existing Postman collection and add the requests below.

Payments are **a local demonstration only**: no provider, no money movement, no card details, no refunds, and no seller payouts. Shipping records seller dispatch; it does not call a courier.

## 1. Start the updated application

Run in PowerShell from your ECommerce folder. Stop the old API first with Ctrl+C.

```powershell
cd 'C:\Users\VaibhavJain\Desktop\Week 1\C#\Capstone 1\ECommerce'
dotnet build '.\ECommerce.sln'
dotnet user-secrets set 'DemoPayments:Enabled' 'true' --project '.\src\ECommerce.Api\ECommerce.Api.csproj'
```

You run database migrations manually. First check whether this migration already exists:

```powershell
Get-ChildItem '.\src\ECommerce.Infrastructure\Persistence\Migrations' -Filter '*_AddDemoPaymentsAndFulfillment.cs'
```

Only if that command finds no file, create it:

```powershell
dotnet ef migrations add AddDemoPaymentsAndFulfillment --project '.\src\ECommerce.Infrastructure\ECommerce.Infrastructure.csproj' --startup-project '.\src\ECommerce.Api\ECommerce.Api.csproj' --output-dir 'Persistence\Migrations' -- --environment Development
```

Review the generated migration, then apply it and start the API:

```powershell
dotnet ef database update --project '.\src\ECommerce.Infrastructure\ECommerce.Infrastructure.csproj' --startup-project '.\src\ECommerce.Api\ECommerce.Api.csproj' -- --environment Development
dotnet run --project '.\src\ECommerce.Api\ECommerce.Api.csproj' --launch-profile https
```

The demo payment feature requires BOTH the Development environment and `DemoPayments:Enabled=true`. It is unavailable in Production even if that setting is true.

## 2. Postman setup and role model

Set `baseUrl` to the HTTPS URL printed by your API, typically `https://localhost:7042`. Every path below starts after `{{baseUrl}}`.

For JSON requests, use **Body → raw → JSON** and `Content-Type: application/json`. Where the table says **none**, select Body → none; do not send `null`.

Useful variables:

```text
baseUrl
customerToken
ownerToken
managerToken
staffToken
adminToken
sellerId
productId
variantId
listingId
listingRowVersion
warehouseId
inventoryItemId
inventoryRowVersion
memberId
orderId
paymentId
checkoutKey
paymentKey
cartRowVersion
cartTotal
cartCurrency
demoPassword
```

Authenticate with the same `POST /api/auth/login` for every person. Save the returned `accessToken` into that person's token variable. For protected requests, select **Authorization → Bearer Token → {{ownerToken}}**, or the appropriate token.

| Identity/access | What it grants |
| --- | --- |
| Anonymous | Registration/login, shared catalog, purchasable storefront, health |
| General user/customer | Own cart/orders/demo payments; create a seller; accept own invitations |
| Seller Owner | Manage that seller's team/roles/assignments and all seller operations |
| Seller Manager | Listings, warehouses, inventory, seller orders, dispatch; no team/role administration |
| WarehouseStaff | Read assigned warehouses and manage inventory in those assigned warehouses only |
| PlatformAdmin | Approve sellers/listings and manage the shared product catalog |

A user can buy products and work for several sellers. A role belongs to a particular seller, not every seller. PlatformAdmin does not automatically grant seller Owner/Manager access.

Seller roles and warehouse assignments are checked against the database on each request. Changes apply with an existing, unexpired JWT: no new login is needed solely because a seller role changed. Expired tokens still require login. Suspended/removed memberships and inactive accounts cannot use their old seller access.

The built-in roles are fixed: `Owner`, `Manager`, `WarehouseStaff`. Custom role/permission editing is not part of this MVP.

## 3. Anonymous APIs

| Method | Path | Body |
| --- | --- | --- |
| GET | `/health` | none |
| GET | `/openapi/v1.json` | none; Development only |
| POST | `/api/auth/register` | Register below |
| POST | `/api/auth/login` | Login below |
| GET | `/api/catalog/products?search=iphone&page=1&pageSize=20` | none |
| GET | `/api/catalog/products/{{productId}}` | none |
| GET | `/api/storefront/listings?search=iphone&page=1&pageSize=20` | none |
| GET | `/api/storefront/listings/{{listingId}}` | none |

Register (use separate email addresses for customer, seller owner, manager, and staff):

```json
{
  "firstName": "Rahul",
  "lastName": "Sharma",
  "email": "rahul@example.com",
  "password": "{{demoPassword}}"
}
```

Login:

```json
{
  "email": "rahul@example.com",
  "password": "{{demoPassword}}"
}
```

Public registration does not let the caller choose PlatformAdmin. Use your existing seeded administrator account for admin requests.

Catalog browsing returns active products and active variants even when nobody has listed them or stock is zero. Search matches product title/brand and active variant name/code/GTIN. Page defaults to 1, pageSize to 20; pageSize is at most 100 and search at most 100 characters.

A catalog product includes:

```json
{
  "productId": "7566ca78-48ca-435f-8b7f-87f99b89aee3",
  "title": "Apple iPhone 17",
  "brandName": "Apple",
  "description": "Shared catalog product",
  "variants": [
    {
      "variantId": "8233b519-a3b7-4dd2-a30d-2c52a334c95b",
      "name": "Black / 128 GB",
      "variantCode": "BLACK-128",
      "gtin": null
    }
  ]
}
```

Use the actual `variants[].variantId` from your response as `variantId` when creating a seller listing. The IDs above are illustrative; do not assume they exist in a different database.

Storefront browsing is different: it returns purchasable seller listings, requiring active seller/product/variant/listing, active warehouse, and available stock.

## 4. General authenticated user and invitation APIs

Use the logged-in person's token.

| Method | Path | Body | Purpose |
| --- | --- | --- | --- |
| GET | `/api/auth/me` | none | Current account |
| POST | `/api/sellers` | Create seller below | Create business and become its Owner |
| GET | `/api/sellers/mine` | none | My seller memberships/roles |
| GET | `/api/seller-invitations` | none | My pending invitations |
| POST | `/api/sellers/{{sellerId}}/invitations/accept` | none | Accept my own invitation |

Create seller:

```json
{
  "displayName": "Vaibhav Electronics",
  "legalBusinessName": "Vaibhav Electronics Private Limited"
}
```

Invitation acceptance uses the invited user's token, NOT the owner's token. Invitations are stored in the application; this MVP does not send invitation emails.

## 5. Customer cart and order APIs

Use `{{customerToken}}`. The server gets the customer ID from the JWT; never send another customer's ID.

| Method | Path | Body |
| --- | --- | --- |
| GET | `/api/cart` | none |
| PUT | `/api/cart/items/{{listingId}}` | Set cart quantity below |
| DELETE | `/api/cart/items/{{listingId}}` | none |
| DELETE | `/api/cart` | none |
| POST | `/api/orders/checkout` | Checkout below + GUID Idempotency-Key header |
| GET | `/api/orders?page=1&pageSize=20` | none |
| GET | `/api/orders/{{orderId}}` | none |
| POST | `/api/orders/{{orderId}}/cancel` | none; unpaid orders only |

Set cart quantity (sets the absolute quantity, not a quantity to add):

```json
{
  "quantity": 2
}
```

Quantity must be 1–99. GET the cart immediately before checkout; copy its `rowVersion`, `totalAmount`, and `currencyCode` to `cartRowVersion`, `cartTotal`, and `cartCurrency`.

For checkout, add:

```text
Idempotency-Key: {{checkoutKey}}
```

Generate a GUID once in PowerShell and save it as `checkoutKey`:

```powershell
[guid]::NewGuid().ToString()
```

Checkout body (`cartTotal` must contain a number, with no quotes around its placeholder):

```json
{
  "cartRowVersion": "{{cartRowVersion}}",
  "expectedTotalAmount": {{cartTotal}},
  "currencyCode": "{{cartCurrency}}",
  "recipientName": "Vaibhav Jain",
  "phone": "9999999999",
  "shippingAddress": {
    "line1": "21 Main Road",
    "line2": null,
    "city": "Delhi",
    "stateOrProvince": "Delhi",
    "postalCode": "110001",
    "countryCode": "IN"
  }
}
```

Checkout returns 201 with an `orderId` and `PendingPayment` status. It calculates prices on the server, snapshots the order, reserves inventory, and clears the cart atomically.

Retrying the same checkout requires the exact same body and key; it returns the existing order with 200. A different checkout needs a new key. Do not use Postman's dynamic random GUID directly in the header when demonstrating retries, because it would change on every send.

Unpaid orders expire after 30 minutes; the background worker releases reservations. Cancellation releases them too. Paid or dispatched orders cannot use this cancellation API; refunds/returns are not implemented.

## 6. Customer demo-payment APIs

Use the same customer's token that owns the order.

| Method | Path | Body |
| --- | --- | --- |
| POST | `/api/orders/{{orderId}}/payments` | none + GUID Idempotency-Key header |
| GET | `/api/orders/{{orderId}}/payments` | none |
| GET | `/api/payments/{{paymentId}}` | none |
| POST | `/api/payments/{{paymentId}}/demo-complete` | Demo outcome below |

Create an attempt with a separate GUID variable `paymentKey`:

```text
Idempotency-Key: {{paymentKey}}
```

Save the response's `paymentId`. The amount/currency come from the saved order; the client cannot choose a different charge amount.

Simulate successful payment:

```json
{
  "outcome": "Succeeded"
}
```

Or simulate failure:

```json
{
  "outcome": "Failed"
}
```

These are simulated outcomes, not bank/gateway confirmations. A successful demo payment marks the order Paid; reserved stock remains held until the seller records dispatch. A failed attempt is terminal for that attempt; start a new attempt with a new payment key while the order is still eligible.

Reuse a creation key only to retry that same attempt. Repeating its same completion outcome is safe; trying to turn a completed failure into success is rejected. Use another unpaid order to demonstrate the failure path after demonstrating success.

