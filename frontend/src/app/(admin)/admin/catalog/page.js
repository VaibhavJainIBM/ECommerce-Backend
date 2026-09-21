import CatalogWorkspace from "@/features/admin/components/CatalogWorkspace";
import styles from "@/features/admin/components/AdminPage.module.css";

export const metadata = {
  title: "Shared catalog",
  description: "Create, activate, and import products and variants.",
};

export default function AdminCatalogPage() {
  return (
    <section className={`site-container ${styles.page}`}>
      <header className={styles.heading}>
        <p className={styles.eyebrow}>Catalog governance</p>
        <h1 className={styles.title}>Shared catalog.</h1>
        <p className={styles.description}>
          Create canonical products and variants for every seller. Keep work
          in draft while reviewing it, then activate it for listing creation.
        </p>
      </header>

      <div className={styles.workspace}>
        <CatalogWorkspace />
      </div>
    </section>
  );
}
