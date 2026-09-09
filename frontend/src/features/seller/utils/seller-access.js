export function hasSellerRole(seller, role) {
  return Boolean(
    seller?.roles?.some(
      (value) => value.toLowerCase() === role.toLowerCase()
    )
  );
}

export function isSellerOwner(seller) {
  return hasSellerRole(seller, "Owner");
}

export function canManageSeller(seller) {
  return isSellerOwner(seller) || hasSellerRole(seller, "Manager");
}

export function canManageInventory(seller) {
  return canManageSeller(seller) || hasSellerRole(seller, "WarehouseStaff");
}

export function sellerStatusMessage(status) {
  switch (status) {
    case "PendingVerification":
      return "Finish your setup, then send the business for platform review.";
    case "UnderReview":
      return "The platform team is reviewing this seller. Draft setup can continue.";
    case "Active":
      return "Approved and ready to publish listings and fulfil paid orders.";
    case "Rejected":
      return "Update the seller setup and submit it for another review.";
    case "Suspended":
      return "This seller is suspended. Operational changes may be restricted.";
    case "Closed":
      return "This seller account is closed.";
    default:
      return "Seller status is unavailable.";
  }
}

export function titleCaseStatus(status) {
  return String(status ?? "Unknown")
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/_/g, " ");
}
