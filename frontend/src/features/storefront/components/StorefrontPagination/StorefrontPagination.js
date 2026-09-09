import Link from "next/link";
import { createProductsHref } from "@/features/storefront/utils/storefront-query";
import styles from "./StorefrontPagination.module.css";

export default function StorefrontPagination({
  currentPage,
  totalPages,
  filters,
}) {
  if (totalPages <= 1) {
    return null;
  }

  const hasPreviousPage = currentPage > 1;
  const hasNextPage = currentPage < totalPages;

  return (
    <nav
      className={styles.pagination}
      aria-label="Product pagination"
    >
      {hasPreviousPage ? (
        <Link
          className={styles.link}
          href={createProductsHref(filters, {
            page: currentPage - 1,
          })}
        >
          ← Previous
        </Link>
      ) : (
        <span className={styles.disabled}>← Previous</span>
      )}

      <p className={styles.status}>
        Page {currentPage} of {totalPages}
      </p>

      {hasNextPage ? (
        <Link
          className={styles.link}
          href={createProductsHref(filters, {
            page: currentPage + 1,
          })}
        >
          Next →
        </Link>
      ) : (
        <span className={styles.disabled}>Next →</span>
      )}
    </nav>
  );
}
