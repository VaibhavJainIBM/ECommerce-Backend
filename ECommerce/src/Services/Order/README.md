# Order service

This service owns order lifecycle data: immutable checkout snapshots, customer order reads,
cancellation, payment state, and payment expiry. It uses `ECommerceOrderDb` and never reads
another service's database.

For the current MVP, checkout accepts product, seller, and unit-price snapshots. The service
recalculates every line total and the order total server-side, but the supplied unit price is a
temporary trust boundary. Before production, replace it with a Product service quote/reservation
call so the Order service receives a signed or server-to-server validated price and stock hold.

`POST /api/orders` and `POST /api/orders/checkout` are aliases. Both require a GUID
`Idempotency-Key` header. `POST /api/orders/{orderId}/payment-confirmation` intentionally matches
the existing Payment service client contract.
