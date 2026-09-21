import { NextResponse } from "next/server";
import { authenticatedApiFetch } from "@/lib/auth/authenticated-api-fetch";
import {
  adminErrorResponse,
  badAdminRequest,
} from "@/features/admin/api/admin-route-response";
import { isGuid } from "@/features/admin/api/admin-validation";

export async function POST(_request, { params }) {
  const { sellerId } = await params;

  if (!isGuid(sellerId)) {
    return badAdminRequest("Enter a valid seller ID.");
  }

  try {
    const seller = await authenticatedApiFetch(
      `/api/admin/sellers/${encodeURIComponent(sellerId)}/approve`,
      { method: "POST" }
    );

    return NextResponse.json(seller);
  } catch (error) {
    return adminErrorResponse(error, "We could not approve this seller.");
  }
}
