import "server-only";
import { apiFetch } from "@/lib/api/api-fetch";

export function getStorefrontListings({
  search = "",
  brand = "",
  minPrice = "",
  maxPrice = "",
  sort = "name_asc",
  page = 1,
  pageSize = 12,
} = {}) {
  const query = new URLSearchParams();

  query.set("page", String(page));
  query.set("pageSize", String(pageSize));

  if (search) {
    query.set("search", search);
  }

  if (brand) query.set("brand", brand);
  if (minPrice) query.set("minPrice", minPrice);
  if (maxPrice) query.set("maxPrice", maxPrice);
  if (sort) query.set("sort", sort);

  return apiFetch(
    `/api/storefront/listings?${query.toString()}`,
    {
      next: {
        revalidate: 30,
        tags: ["storefront-listings"],
      },
    }
  );
}

export function getStorefrontListing(listingId) {
  return apiFetch(`/api/storefront/listings/${listingId}`, {
    next: {
      revalidate: 30,
      tags: [`storefront-listing-${listingId}`],
    },
  });
}
