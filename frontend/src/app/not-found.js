import Link from "next/link";
import styles from "./not-found.module.css";

export default function NotFound() {
  return (
    <main id="main-content" className={styles.page}>
      <section className={styles.content}>
        <p className={styles.code}>404</p>
        <h1 className={styles.title}>This page could not be found.</h1>
        <p className={styles.description}>
          The address may be incorrect or the page may have moved.
        </p>
        <Link className={styles.link} href="/">
          Return home
        </Link>
      </section>
    </main>
  );
}
