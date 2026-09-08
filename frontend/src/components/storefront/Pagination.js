import Link from "next/link";
import styles from "./Pagination.module.css";

function createPageHref(page, search) {
  const query = new URLSearchParams();

  query.set("page", String(page));

  if (search) {
    query.set("search", search);
  }

  return `/?${query.toString()}`;
}

export default function Pagination({
  currentPage,
  totalPages,
  search,
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
          href={createPageHref(currentPage - 1, search)}
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
          href={createPageHref(currentPage + 1, search)}
        >
          Next →
        </Link>
      ) : (
        <span className={styles.disabled}>Next →</span>
      )}
    </nav>
  );
}