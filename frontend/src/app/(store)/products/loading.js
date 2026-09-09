import styles from "./loading.module.css";

export default function ProductsLoading() {
  return (
    <section
      className={`site-container ${styles.loading}`}
      role="status"
      aria-label="Loading products"
    >
      <p className={styles.label}>Loading products</p>

      <div className={styles.grid}>
        {Array.from({ length: 6 }).map((_, index) => (
          <div className={styles.item} key={index}>
            <div className={styles.image} />
            <div className={styles.title} />
            <div className={styles.text} />
          </div>
        ))}
      </div>
    </section>
  );
}
