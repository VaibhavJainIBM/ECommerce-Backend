import SellerApprovalForm from "@/features/admin/components/SellerApprovalForm";
import styles from "@/features/admin/components/AdminPage.module.css";

export const metadata = {
  title: "Seller approval",
  description: "Approve marketplace sellers after verification.",
};

export default function AdminSellersPage() {
  return (
    <section className={`site-container ${styles.page}`}>
      <header className={styles.heading}>
        <p className={styles.eyebrow}>Marketplace access</p>
        <h1 className={styles.title}>Seller approval.</h1>
        <p className={styles.description}>
          Complete the final lifecycle step for a verified seller. Successful
          approval changes the seller from UnderReview to Active.
        </p>
      </header>

      <p className={styles.notice}>
        The current backend provides the approval command but not an admin
        seller queue. Use the seller ID produced by onboarding; no unsupported
        status or rejection action is shown here.
      </p>

      <div className={styles.workspace}>
        <SellerApprovalForm />
      </div>
    </section>
  );
}
