import styles from "./loading.module.css";

export default function AdminLoading() {
  return (
    <section className={`site-container ${styles.page}`} aria-busy="true">
      <span className={styles.eyebrow} />
      <span className={styles.title} />
      <span className={styles.copy} />
      <div className={styles.grid}>
        <span />
        <span />
        <span />
      </div>
      <span className="sr-only">Loading administration workspace…</span>
    </section>
  );
}
