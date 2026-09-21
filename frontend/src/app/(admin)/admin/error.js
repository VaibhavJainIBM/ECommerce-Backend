"use client";

import Link from "next/link";
import styles from "./error.module.css";

export default function AdminError({ error, reset }) {
  console.error(error);

  return (
    <section className={`site-container ${styles.page}`}>
      <p className={styles.eyebrow}>Admin service unavailable</p>
      <h1 className={styles.title}>The workspace could not be loaded.</h1>
      <p className={styles.description}>
        The API may be temporarily unavailable. Retry the request or return to
        the storefront.
      </p>
      <div className={styles.actions}>
        <button type="button" onClick={reset}>
          Try again
        </button>
        <Link href="/">View storefront</Link>
      </div>
    </section>
  );
}
