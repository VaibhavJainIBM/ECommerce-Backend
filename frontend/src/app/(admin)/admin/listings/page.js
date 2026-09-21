import ListingApprovalForm from "@/features/admin/components/ListingApprovalForm";
import styles from "@/features/admin/components/AdminPage.module.css";

export const metadata = {
  title: "Listing approval",
  description: "Approve seller listings for publication.",
};

export default function AdminListingsPage() {
  return (
    <section className={`site-container ${styles.page}`}>
      <header className={styles.heading}>
        <p className={styles.eyebrow}>Marketplace quality</p>
        <h1 className={styles.title}>Listing approval.</h1>
        <p className={styles.description}>
          Publish a listing submitted by an active seller. Approval requires
          the latest row version so stale reviews cannot overwrite changes.
        </p>
      </header>

      <p className={styles.notice}>
        The current backend provides the approval command but not an admin
        listing queue. Use the seller ID, listing ID, and row version returned
        when the seller submits the listing for review.
      </p>

      <div className={styles.workspace}>
        <ListingApprovalForm />
      </div>
    </section>
  );
}
