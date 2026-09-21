import Link from "next/link";
import styles from "./page.module.css";

export default function AdminOverviewPage() {
  return (
    <section className={`site-container ${styles.page}`}>
      <header className={styles.hero}>
        <p className={styles.eyebrow}>Platform operations</p>
        <h1 className={styles.title}>Review. Curate. Publish.</h1>
        <p className={styles.description}>
          Approve marketplace participants and listings, then maintain the
          shared product catalog that powers every storefront offer.
        </p>
      </header>

      <div className={styles.cards}>
        <Link className={styles.card} href="/admin/sellers">
          <span className={styles.cardNumber}>01</span>
          <h2>Seller approval</h2>
          <p>Activate a seller after profile verification and review.</p>
          <span className={styles.cardAction}>Open seller desk →</span>
        </Link>

        <Link className={styles.card} href="/admin/listings">
          <span className={styles.cardNumber}>02</span>
          <h2>Listing approval</h2>
          <p>Publish a seller listing using its latest concurrency token.</p>
          <span className={styles.cardAction}>Open listing desk →</span>
        </Link>

        <Link className={styles.card} href="/admin/catalog">
          <span className={styles.cardNumber}>03</span>
          <h2>Shared catalog</h2>
          <p>Create, activate, or bulk-import products and variants.</p>
          <span className={styles.cardAction}>Open catalog desk →</span>
        </Link>
      </div>

      <section className={styles.sequence}>
        <p className={styles.eyebrow}>Marketplace sequence</p>
        <ol>
          <li>
            <span>1</span>
            Curate and activate the shared product and its variants.
          </li>
          <li>
            <span>2</span>
            Approve a seller after the seller submits for review.
          </li>
          <li>
            <span>3</span>
            Approve the seller listing after it reaches PendingReview.
          </li>
        </ol>
      </section>
    </section>
  );
}
