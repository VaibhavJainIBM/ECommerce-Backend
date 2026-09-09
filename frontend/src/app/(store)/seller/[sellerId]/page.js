import Link from "next/link";
import { requireSeller } from "@/features/seller/api/require-seller";
import SellerLifecycleAction from "@/features/seller/components/SellerLifecycleAction";
import {
  canManageInventory,
  canManageSeller,
  isSellerOwner,
  sellerStatusMessage,
  titleCaseStatus,
} from "@/features/seller/utils/seller-access";
import styles from "@/features/seller/components/SellerPortal.module.css";

export default async function SellerOverviewPage({ params }) {
  const { sellerId } = await params;
  const seller = await requireSeller(sellerId);
  const canManage = canManageSeller(seller);
  const canUseInventory = canManageInventory(seller);
  const isOwner = isSellerOwner(seller);
  const canSubmit =
    isOwner && ["PendingVerification", "Rejected"].includes(seller.sellerStatus);

  return (
    <main className={styles.page}>
      <header className={styles.pageHeader}>
        <p className={styles.eyebrow}>Overview</p>
        <h1 className={styles.pageTitle}>Seller operations.</h1>
        <p className={styles.description}>
          {sellerStatusMessage(seller.sellerStatus)}
        </p>
      </header>

      <div className={styles.sections}>
        <section className={styles.panel}>
          <div className={styles.cardHeader}>
            <div>
              <p className={styles.sectionLabel}>Approval lifecycle</p>
              <h2 className={styles.sectionTitle}>
                {titleCaseStatus(seller.sellerStatus)}
              </h2>
            </div>
            <span className={styles.status}>
              {titleCaseStatus(seller.sellerStatus)}
            </span>
          </div>
          <p className={styles.cardCopy}>
            {sellerStatusMessage(seller.sellerStatus)}
          </p>
          {canSubmit && <SellerLifecycleAction sellerId={sellerId} />}
          {seller.sellerStatus === "UnderReview" && (
            <p className={styles.message}>
              No action is needed while the platform team reviews the seller.
            </p>
          )}
        </section>

        <section className={styles.panel}>
          <p className={styles.sectionLabel}>Your access</p>
          <h2 className={styles.sectionTitle}>Available work areas.</h2>
          <div className={styles.roles}>
            {seller.roles.map((role) => (
              <span className={styles.role} key={role}>{role}</span>
            ))}
          </div>
          <div className={styles.cards}>
            {canManage && (
              <Link className={styles.card} href={`/seller/${sellerId}/listings`}>
                <h3 className={styles.cardTitle}>Listings</h3>
                <p className={styles.cardCopy}>
                  Create drafts, set prices, and submit listings for approval.
                </p>
              </Link>
            )}
            {canUseInventory && (
              <Link className={styles.card} href={`/seller/${sellerId}/inventory`}>
                <h3 className={styles.cardTitle}>Inventory</h3>
                <p className={styles.cardCopy}>
                  Receive stock and keep warehouse quantities accurate.
                </p>
              </Link>
            )}
            {isOwner && (
              <Link className={styles.card} href={`/seller/${sellerId}/team`}>
                <h3 className={styles.cardTitle}>Team</h3>
                <p className={styles.cardCopy}>
                  Invite members, assign roles, and scope warehouse access.
                </p>
              </Link>
            )}
            {canManage && (
              <Link className={styles.card} href={`/seller/${sellerId}/orders`}>
                <h3 className={styles.cardTitle}>Fulfilment</h3>
                <p className={styles.cardCopy}>
                  Review seller order lines and dispatch paid orders.
                </p>
              </Link>
            )}
          </div>
        </section>
      </div>
    </main>
  );
}
