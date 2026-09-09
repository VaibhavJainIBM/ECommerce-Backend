import Link from "next/link";
import styles from "./EmptyCatalog.module.css";

export default function EmptyCatalog({ search }) {
  return (
    <div className={styles.empty}>
      <p className={styles.eyebrow}>Nothing found</p>

      <h3 className={styles.title}>
        {search
          ? `No products matched “${search}”.`
          : "The product catalog is currently empty."}
      </h3>

      <p className={styles.description}>
        {search
          ? "Try a product name, brand, variant, or seller."
          : "Active products will appear here when sellers publish them."}
      </p>

      {search && (
        <Link className={styles.link} href="/products">
          View all products
        </Link>
      )}
    </div>
  );
}
