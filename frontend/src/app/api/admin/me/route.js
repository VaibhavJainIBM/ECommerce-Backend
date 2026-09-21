import { NextResponse } from "next/server";
import { authenticatedApiFetch } from "@/lib/auth/authenticated-api-fetch";
import { adminErrorResponse } from "@/features/admin/api/admin-route-response";

export async function GET() {
  try {
    const profile = await authenticatedApiFetch("/api/admin/me");

    return NextResponse.json(profile);
  } catch (error) {
    return adminErrorResponse(
      error,
      "The administrator profile is currently unavailable."
    );
  }
}
