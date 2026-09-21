import "server-only";

import { authenticatedApiFetch } from "@/lib/auth/authenticated-api-fetch";

export function getAdminProfile() {
  return authenticatedApiFetch("/api/admin/me", {
    cache: "no-store",
  });
}
