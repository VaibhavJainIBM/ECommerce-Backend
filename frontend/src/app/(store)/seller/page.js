import { getSellerInvitations } from "@/features/seller/api/seller-api";
import { requireMySellers } from "@/features/seller/api/require-seller";
import SellerStart from "@/features/seller/components/SellerStart";
import styles from "@/features/seller/components/SellerPortal.module.css";

export const metadata = {
  title: "Seller centre",
  description: "Create or manage your marketplace seller accounts.",
};

export default async function SellerPage() {
  const sellers = await requireMySellers();
  const invitations = await getSellerInvitations();

  return (
    <section className={`site-container ${styles.page}`}>
      <header className={styles.pageHeader}>
        <p className={styles.eyebrow}>Seller centre</p>
        <h1 className={styles.pageTitle}>Build your marketplace presence.</h1>
        <p className={styles.description}>
          Start a seller account, accept team invitations, and enter an
          existing workspace from one place.
        </p>
      </header>

      <SellerStart sellers={sellers} invitations={invitations} />
    </section>
  );
}
