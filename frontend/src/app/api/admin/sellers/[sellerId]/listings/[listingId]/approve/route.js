import { NextResponse } from "next/server";
import { authenticatedApiFetch } from "@/lib/auth/authenticated-api-fetch";
import {
  adminErrorResponse,
  badAdminRequest,
} from "@/features/admin/api/admin-route-response";
import { isGuid } from "@/features/admin/api/admin-validation";

export async function POST(request, { params }) {
  const { sellerId, listingId } = await params;

  if (!isGuid(sellerId)) {
    return badAdminRequest("Enter a valid seller ID.");
  }

  if (!isGuid(listingId)) {
    return badAdminRequest("Enter a valid listing ID.");
  }

  let payload;

  try {
    payload = await request.json();
  } catch {
    return badAdminRequest("The request body must contain valid JSON.");
  }

  if (typeof payload?.rowVersion !== "string" || !payload.rowVersion.trim()) {
    return badAdminRequest("The latest listing row version is required.");
  }

  try {
    const listing = await authenticatedApiFetch(
      `/api/admin/sellers/${encodeURIComponent(sellerId)}/listings/${encodeURIComponent(listingId)}/approve`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          rowVersion: payload.rowVersion.trim(),
        }),
      }
    );

    return NextResponse.json(listing);
  } catch (error) {
    return adminErrorResponse(error, "We could not approve this listing.");
  }
}
