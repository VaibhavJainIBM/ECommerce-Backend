import Link from "next/link";
import MobileMenu from "../MobileMenu/MobileMenu";
import styles from "./StoreHeader.module.css";

export default function StoreHeader() {
  return (
    <header className={styles.header}>
      <div className={`site-container ${styles.inner}`}>
        <nav
          className={styles.desktopNavigation}
          aria-label="Primary navigation"
        >
          <Link className={styles.navigationLink} href="/">
            Home
          </Link>

          <Link
            className={styles.navigationLink}
            href="/products"
          >
            Products
          </Link>
        </nav>

        <MobileMenu />
      </div>
    </header>
  );
}
