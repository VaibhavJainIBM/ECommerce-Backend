import "server-only";

import { redirect } from "next/navigation";
import { ApiError } from "@/lib/api/api-error";
import { getMySellers } from "@/features/seller/api/seller-api";
import { hasSellerRole } from "@/features/seller/utils/seller-access";

export async function requireMySellers() {
  let sellers;

  try {
    sellers = await getMySellers();
  } catch (error) {
    if (!(error instanceof ApiError) || error.status !== 401) {
      throw error;
    }
  }

  if (!sellers) {
    redirect("/login?next=/seller");
  }

  return sellers;
}

export async function requireSeller(sellerId, allowedRoles = []) {
  const sellers = await requireMySellers();
  const seller = sellers.find(
    (item) => item.sellerId.toLowerCase() === sellerId.toLowerCase()
  );

  if (!seller || seller.memberStatus !== "Active") {
    redirect("/seller");
  }

  if (
    allowedRoles.length > 0 &&
    !allowedRoles.some((role) => hasSellerRole(seller, role))
  ) {
    redirect(`/seller/${encodeURIComponent(sellerId)}`);
  }

  return seller;
}
