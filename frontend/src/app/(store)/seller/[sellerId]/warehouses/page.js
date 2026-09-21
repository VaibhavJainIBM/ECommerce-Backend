import { getSellerWarehouses } from "@/features/seller/api/seller-api";
import { requireSeller } from "@/features/seller/api/require-seller";
import WarehouseManager from "@/features/seller/components/WarehouseManager";
import { canManageSeller } from "@/features/seller/utils/seller-access";
import styles from "@/features/seller/components/SellerPortal.module.css";

export const metadata = { title: "Seller warehouses" };

export default async function SellerWarehousesPage({ params }) {
  const { sellerId } = await params;
  const seller = await requireSeller(sellerId, ["Owner", "Manager", "WarehouseStaff"]);
  const warehouses = await getSellerWarehouses(sellerId);

  return (
    <main className={styles.page}>
      <header className={styles.pageHeader}>
        <p className={styles.eyebrow}>Fulfilment network</p>
        <h1 className={styles.pageTitle}>Warehouses.</h1>
        <p className={styles.description}>
          Managers create and activate locations. Warehouse staff see only
          the locations assigned to their membership.
        </p>
      </header>
      <WarehouseManager
        sellerId={sellerId}
        sellerStatus={seller.sellerStatus}
        canManage={canManageSeller(seller)}
        warehouses={warehouses}
      />
    </main>
  );
}
