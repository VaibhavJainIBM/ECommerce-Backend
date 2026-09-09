import Link from "next/link";
import styles from "./home.module.css";

export const metadata = {
  title: "Home",
  description: "Browse the available product catalog.",
};

export default function HomePage() {
  return (
    <section className={styles.hero}>
      <div className={`site-container ${styles.heroInner}`}>
        <p className={styles.eyebrow}>Storefront</p>
        <h1 className={styles.title}>Browse available products.</h1>
        <p className={styles.description}>
          Search the catalog and view current prices, sellers, variants,
          and stock.
        </p>
        <Link className={styles.primaryAction} href="/products">
          View products
        </Link>
      </div>
    </section>
  );
}
