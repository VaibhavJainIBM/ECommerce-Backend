import "server-only";

import { cookies } from "next/headers";
import { apiFetch } from "@/lib/api/api-fetch";
import { ApiError } from "@/lib/api/api-error";
import { AUTH_COOKIE_NAME } from "@/lib/auth/auth-cookie";

export async function authenticatedApiFetch(
  path,
  options = {}
) {
  const cookieStore = await cookies();
  const accessToken =
    cookieStore.get(AUTH_COOKIE_NAME)?.value;

  if (!accessToken) {
    throw new ApiError(
      "Authentication is required.",
      401
    );
  }

  return apiFetch(path, {
    ...options,
    cache: options.cache ?? "no-store",
    headers: {
      ...options.headers,
      Authorization: `Bearer ${accessToken}`,
    },
  });
}