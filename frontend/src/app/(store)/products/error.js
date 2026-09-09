"use client";

import { useEffect } from "react";
import styles from "./error.module.css";

export default function ProductsError({ error, reset }) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <section className={`site-container ${styles.error}`}>
      <p className={styles.label}>Storefront unavailable</p>
      <h1 className={styles.title}>We couldn’t load the products.</h1>
      <p className={styles.description}>
        The service may be temporarily unavailable. Please try again.
      </p>
      <button className={styles.button} type="button" onClick={reset}>
        Try again
      </button>
    </section>
  );
}
