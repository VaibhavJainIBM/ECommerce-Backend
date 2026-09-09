import "server-only";

import { authenticatedApiFetch } from "@/lib/auth/authenticated-api-fetch";
import { ApiError } from "@/lib/api/api-error";

export async function getCurrentUser() {
  try {
    return await authenticatedApiFetch("/api/auth/me");
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      return null;
    }

    console.error("Unable to load the current user:", error);
    return null;
  }
}

export function getUserDisplayName(user) {
  if (!user) {
    return "";
  }

  const fullName = [user.firstName, user.lastName]
    .filter(Boolean)
    .join(" ")
    .trim();

  return fullName || user.email || "Account";
}