import { NextResponse } from "next/server";
import { authenticatedApiFetch } from "@/lib/auth/authenticated-api-fetch";
import {
  adminErrorResponse,
  badAdminRequest,
} from "@/features/admin/api/admin-route-response";
import { isGuid } from "@/features/admin/api/admin-validation";

export async function POST(_request, { params }) {
  const { productId } = await params;

  if (!isGuid(productId)) {
    return badAdminRequest("Enter a valid product ID.");
  }

  try {
    const product = await authenticatedApiFetch(
      `/api/admin/catalog/products/${encodeURIComponent(productId)}/activate`,
      { method: "POST" }
    );

    return NextResponse.json(product);
  } catch (error) {
    return adminErrorResponse(error, "We could not activate this product.");
  }
}
