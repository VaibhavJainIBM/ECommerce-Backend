import { NextResponse } from "next/server";
import { authenticatedApiFetch } from "@/lib/auth/authenticated-api-fetch";
import {
  adminErrorResponse,
  badAdminRequest,
} from "@/features/admin/api/admin-route-response";

function isNonEmptyText(value) {
  return typeof value === "string" && Boolean(value.trim());
}

export async function POST(request) {
  let payload;

  try {
    payload = await request.json();
  } catch {
    return badAdminRequest("The request body must contain valid JSON.");
  }

  if (!isNonEmptyText(payload?.title)) {
    return badAdminRequest("Product title is required.");
  }

  if (!isNonEmptyText(payload?.brandName)) {
    return badAdminRequest("Brand name is required.");
  }

  if (!Array.isArray(payload?.variants) || payload.variants.length === 0) {
    return badAdminRequest("At least one product variant is required.");
  }

  try {
    const product = await authenticatedApiFetch(
      "/api/admin/catalog/products",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          title: payload.title,
          brandName: payload.brandName,
          description: payload.description ?? null,
          variants: payload.variants,
        }),
      }
    );

    return NextResponse.json(product, { status: 201 });
  } catch (error) {
    return adminErrorResponse(error, "We could not create this product.");
  }
}
