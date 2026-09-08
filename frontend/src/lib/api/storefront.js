import "server-only";

const API_BASE_URL =
  process.env.ECOMMERCE_API_BASE_URL ?? "http://localhost:5167";

async function getErrorMessage(response) {
  try {
    const problem = await response.json();

    return (
      problem.detail ??
      problem.title ??
      "The storefront request failed."
    );
  } catch {
    return "The storefront request failed.";
  }
}

export async function getStorefrontListings({
  search = "",
  page = 1,
  pageSize = 12,
} = {}) {
  const url = new URL(
    "/api/storefront/listings",
    API_BASE_URL
  );

  url.searchParams.set("page", String(page));
  url.searchParams.set("pageSize", String(pageSize));

  if (search) {
    url.searchParams.set("search", search);
  }

  const response = await fetch(url, {
    headers: {
      Accept: "application/json",
    },
    next: {
      revalidate: 30,
      tags: ["storefront-listings"],
    },
  });

  if (!response.ok) {
    const message = await getErrorMessage(response);

    throw new Error(
      `${message} HTTP status: ${response.status}.`
    );
  }

  return response.json();
}