import Link from "next/link";
import { requireSeller } from "@/features/seller/api/require-seller";
import {
  canManageInventory,
  canManageSeller,
  isSellerOwner,
  titleCaseStatus,
} from "@/features/seller/utils/seller-access";
import styles from "@/features/seller/components/SellerPortal.module.css";

export default async function SellerWorkspaceLayout({ children, params }) {
  const { sellerId } = await params;
  const seller = await requireSeller(sellerId);
  const management = canManageSeller(seller);
  const inventory = canManageInventory(seller);
  const owner = isSellerOwner(seller);
  const base = `/seller/${sellerId}`;

  return (
    <div className="site-container">
      <header className={styles.workspaceHeader}>
        <div className={styles.workspaceTopline}>
          <div>
            <p className={styles.eyebrow}>Seller workspace</p>
            <p className={styles.workspaceTitle}>{seller.displayName}</p>
            <p className={styles.workspaceLegal}>{seller.legalBusinessName}</p>
          </div>
          <div className={styles.stack}>
            <span className={styles.status}>
              {titleCaseStatus(seller.sellerStatus)}
            </span>
            <Link className={styles.workspaceSwitch} href="/seller">
              Switch workspace
            </Link>
          </div>
        </div>

        <nav className={styles.workspaceNav} aria-label="Seller workspace">
          <Link href={base}>Overview</Link>
          {management && <Link href={`${base}/listings`}>Listings</Link>}
          {inventory && <Link href={`${base}/warehouses`}>Warehouses</Link>}
          {inventory && <Link href={`${base}/inventory`}>Inventory</Link>}
          {owner && <Link href={`${base}/team`}>Team</Link>}
          {management && <Link href={`${base}/orders`}>Orders</Link>}
        </nav>
      </header>

      {children}
    </div>
  );
}
