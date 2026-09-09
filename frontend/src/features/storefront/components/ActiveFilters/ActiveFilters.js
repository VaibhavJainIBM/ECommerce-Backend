import Link from "next/link";
import { createProductsHref } from "@/features/storefront/utils/storefront-query";
import styles from "./ActiveFilters.module.css";

export default function ActiveFilters({ filters }) {
  const activeFilters = [
    filters.search && {
      key: "search",
      label: `Search: ${filters.search}`,
    },
    filters.brand && {
      key: "brand",
      label: `Brand: ${filters.brand}`,
    },
    filters.minPrice && {
      key: "minPrice",
      label: `Min: ${filters.minPrice}`,
    },
    filters.maxPrice && {
      key: "maxPrice",
      label: `Max: ${filters.maxPrice}`,
    },
  ].filter(Boolean);

  if (activeFilters.length === 0) return null;

  return (
    <div className={styles.filters} aria-label="Active filters">
      <p className={styles.label}>Active filters</p>

      <div className={styles.list}>
        {activeFilters.map((filter) => (
          <Link
            className={styles.chip}
            href={createProductsHref(filters, {
              [filter.key]: "",
              page: 1,
            })}
            key={filter.key}
            aria-label={`Remove ${filter.label}`}
          >
            {filter.label} <span aria-hidden="true">×</span>
          </Link>
        ))}
      </div>
    </div>
  );
}
