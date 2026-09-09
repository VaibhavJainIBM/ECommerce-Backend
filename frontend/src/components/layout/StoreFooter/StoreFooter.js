import Link from "next/link";
import styles from "./StoreFooter.module.css";

export default function StoreFooter() {
  return (
    <footer className={styles.footer}>
      <div className={`site-container ${styles.inner}`}>
        <nav
          className={styles.navigation}
          aria-label="Footer navigation"
        >
          <Link href="/">Home</Link>
          <Link href="/products">Products</Link>
        </nav>
      </div>
    </footer>
  );
}
