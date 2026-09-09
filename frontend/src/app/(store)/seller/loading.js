import styles from "@/features/seller/components/SellerPortal.module.css";

export default function SellerLoading() {
  return (
    <section className={`site-container ${styles.page}`} aria-busy="true">
      <div className={styles.panel}>
        <p className={styles.eyebrow}>Seller centre</p>
        <h1 className={styles.sectionTitle}>Loading your workspace…</h1>
      </div>
    </section>
  );
}
