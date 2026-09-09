"use client";

import styles from "@/features/seller/components/SellerPortal.module.css";

export default function SellerError({ error, reset }) {
  console.error(error);

  return (
    <section className={`site-container ${styles.page}`}>
      <div className={styles.panel}>
        <p className={styles.eyebrow}>Seller centre unavailable</p>
        <h1 className={styles.sectionTitle}>We could not load this workspace.</h1>
        <p className={styles.description}>
          The seller service may be temporarily unavailable. Try again.
        </p>
        <button className={styles.button} type="button" onClick={reset}>
          Try again
        </button>
      </div>
    </section>
  );
}
