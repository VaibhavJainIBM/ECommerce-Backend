import {
  getSellerInventory,
  getSellerListings,
  getSellerWarehouses,
} from "@/features/seller/api/seller-api";
import { requireSeller } from "@/features/seller/api/require-seller";
import InventoryManager from "@/features/seller/components/InventoryManager";
import { canManageSeller } from "@/features/seller/utils/seller-access";
import styles from "@/features/seller/components/SellerPortal.module.css";

export const metadata = { title: "Seller inventory" };

export default async function SellerInventoryPage({ params }) {
  const { sellerId } = await params;
  const seller = await requireSeller(sellerId, ["Owner", "Manager", "WarehouseStaff"]);
  const management = canManageSeller(seller);
  const [inventory, warehouses, listingPage] = await Promise.all([
    getSellerInventory(sellerId),
    getSellerWarehouses(sellerId),
    management
      ? getSellerListings(sellerId, { page: 1, pageSize: 100 })
      : Promise.resolve({ items: [] }),
  ]);

  return (
    <main className={styles.page}>
      <header className={styles.pageHeader}>
        <p className={styles.eyebrow}>Warehouse operations</p>
        <h1 className={styles.pageTitle}>Inventory.</h1>
        <p className={styles.description}>
          Create stock records, receive new units, and reconcile the current
          on-hand quantity. Reserved units are maintained by order processing.
        </p>
      </header>

      <InventoryManager
        sellerId={sellerId}
        sellerStatus={seller.sellerStatus}
        inventory={inventory}
        warehouses={warehouses}
        listings={listingPage.items}
      />
    </main>
  );
}
