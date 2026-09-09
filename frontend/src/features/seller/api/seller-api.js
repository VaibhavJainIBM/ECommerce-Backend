import "server-only";

import { apiFetch } from "@/lib/api/api-fetch";
import { authenticatedApiFetch } from "@/lib/auth/authenticated-api-fetch";

function queryString(values) {
  const query = new URLSearchParams();

  for (const [key, value] of Object.entries(values)) {
    if (value !== undefined && value !== null && value !== "") {
      query.set(key, String(value));
    }
  }

  const serialized = query.toString();
  return serialized ? `?${serialized}` : "";
}

export function getMySellers() {
  return authenticatedApiFetch("/api/sellers/mine");
}

export function getSellerInvitations() {
  return authenticatedApiFetch("/api/seller-invitations");
}

export function getSellerListings(
  sellerId,
  { page = 1, pageSize = 20, status = "" } = {}
) {
  return authenticatedApiFetch(
    `/api/sellers/${encodeURIComponent(sellerId)}/listings${queryString({
      page,
      pageSize,
      status,
    })}`
  );
}

export function getCatalogProducts({ search = "", pageSize = 100 } = {}) {
  return apiFetch(
    `/api/catalog/products${queryString({
      search,
      page: 1,
      pageSize,
    })}`,
    { cache: "no-store" }
  );
}

export function getSellerWarehouses(sellerId) {
  return authenticatedApiFetch(
    `/api/sellers/${encodeURIComponent(sellerId)}/warehouses`
  );
}

export function getSellerInventory(sellerId) {
  return authenticatedApiFetch(
    `/api/sellers/${encodeURIComponent(sellerId)}/inventory`
  );
}

export function getSellerMembers(sellerId) {
  return authenticatedApiFetch(
    `/api/sellers/${encodeURIComponent(sellerId)}/members`
  );
}

export function getSellerRoles(sellerId) {
  return authenticatedApiFetch(
    `/api/sellers/${encodeURIComponent(sellerId)}/roles`
  );
}

export function getSellerOrders(
  sellerId,
  { page = 1, pageSize = 20 } = {}
) {
  return authenticatedApiFetch(
    `/api/sellers/${encodeURIComponent(sellerId)}/orders${queryString({
      page,
      pageSize,
    })}`
  );
}
