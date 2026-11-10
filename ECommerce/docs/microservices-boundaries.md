# E-commerce microservice boundaries

## Target architecture

| Service | Owns | Database | Depends on |
| --- | --- | --- | --- |
| User | ASP.NET Identity, registration/login, JWT issuance, platform roles, sellers, seller members and seller roles | `ECommerceUserDb` | None |
| Product | Products, product variants, catalogue administration and CSV catalogue import | `ECommerceProductDb` | None |
| Order | Seller listings, warehouses, warehouse assignments, inventory, storefront, carts, orders, allocations and fulfilment | `ECommerceOrderDb` | User and Product APIs |
| Payment | Payment attempts and payment-provider integration | `ECommercePaymentDb` | Order API |

Each service owns its schema. A service may store another service's GUID as an external identifier, but it must not create an EF Core relationship or query another service's tables.

## Why marketplace operations belong to Order

The existing checkout validates the listing and live price, reserves inventory, creates the order and allocation rows, and clears the cart in one SQL transaction. With only the four required services, keeping listings, warehouses and inventory with Order preserves that atomic operation. Moving inventory to Product immediately would require a reservation API, an outbox, compensating actions and a checkout saga.

Order items store immutable seller, product, variant, SKU and price snapshots. Product and User remain the sources of truth for their current data, while historical orders remain readable if a product or seller later changes.

## Dependency direction

```text
User      -> no business-service dependency
Product   -> no business-service dependency
Order     -> User + Product
Payment   -> Order
```

This direction avoids circular service calls. User is the only JWT issuer. Every API validates the same issuer, audience and signing key. Seller membership roles are not put permanently into the token; Order asks User for current seller access when a protected seller command is executed.

## Synchronous contracts

Order must preserve the contract already consumed by Payment:

```http
GET  /api/orders/{orderId}
POST /api/orders/{orderId}/payment-confirmation
```

The order response includes `orderId`, `status`, `totalAmount`, `currencyCode` and `expiresAtUtc`. Payment forwards the caller's bearer token. A later hardening step should give Payment a service identity and make payment confirmation an internal-only operation.

The full marketplace extraction also needs these internal contracts:

- User access check: the current member, seller status and seller roles.
- Product variant snapshot: product/variant identifiers, display values and activation status.

Use timeouts and return explicit `503 Service Unavailable` responses when a required dependency cannot be reached. Do not silently read the old monolith database as a fallback.

## Current extraction checkpoint

- Product and Payment are independent services already.
- User is extracted as the JWT/Identity owner.
- Order's independent order lifecycle and Payment-facing contract are extracted first.
- The next migration slice moves seller listings, warehouses, inventory, cart and fulfilment from the old database into Order, then disables the duplicate monolith endpoints.

During migration there must be exactly one writer for each aggregate. Avoid dual-writing the monolith and a service because failures can leave the two databases inconsistent.

## Local ports

| API | HTTPS/HTTP development port |
| --- | --- |
| User | `5201` |
| Product | `5202` |
| Order | `5203` |
| Payment | `5204` |

Payment's local `Services:Order` value must point to `http://localhost:5203/`. In Kubernetes it is overridden with `http://ecommerce-order-service/`.

## Deployment configuration

Never commit connection strings containing credentials or the JWT signing key. The Kubernetes Deployments read them from the `ecommerce-secrets` Secret. Configuration such as issuer, audience and service URLs comes from `ecommerce-config`.
