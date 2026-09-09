"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import {
  createProductsHref,
  DEFAULT_SORT,
  SORT_OPTIONS,
} from "@/features/storefront/utils/storefront-query";
import styles from "./CatalogControls.module.css";

export default function CatalogControls({ initialFilters }) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [error, setError] = useState("");
  const [filters, setFilters] = useState(initialFilters);

  function handleChange(event) {
    const { name, value } = event.target;

    setFilters((currentFilters) => ({
      ...currentFilters,
      [name]: value,
    }));
  }

  function handleSubmit(event) {
    event.preventDefault();

    const minimum = Number(filters.minPrice);
    const maximum = Number(filters.maxPrice);

    if (
      filters.minPrice &&
      filters.maxPrice &&
      minimum > maximum
    ) {
      setError("Minimum price cannot exceed maximum price.");
      return;
    }

    setError("");
    startTransition(() => {
      router.push(createProductsHref(filters, { page: 1 }));
    });
  }

  function handleClear() {
    const clearedFilters = {
      search: "",
      brand: "",
      minPrice: "",
      maxPrice: "",
      sort: DEFAULT_SORT,
      page: 1,
    };

    setFilters(clearedFilters);
    setError("");
    startTransition(() => router.push("/products"));
  }

  const hasChangedFilters =
    filters.search ||
    filters.brand ||
    filters.minPrice ||
    filters.maxPrice ||
    filters.sort !== DEFAULT_SORT;

  return (
    <form className={styles.form} onSubmit={handleSubmit} role="search">
      <div className={styles.headingRow}>
        <div>
          <p className={styles.eyebrow}>Catalog controls</p>
          <h3 className={styles.title}>Find products</h3>
        </div>

        {hasChangedFilters && (
          <button
            className={styles.clearButton}
            type="button"
            onClick={handleClear}
            disabled={isPending}
          >
            Clear all
          </button>
        )}
      </div>

      <div className={styles.fields}>
        <label className={`${styles.field} ${styles.searchField}`}>
          <span>Search</span>
          <input
            name="search"
            type="search"
            value={filters.search}
            maxLength={100}
            placeholder="Product, variant, seller"
            onChange={handleChange}
          />
        </label>

        <label className={styles.field}>
          <span>Brand</span>
          <input
            name="brand"
            type="text"
            value={filters.brand}
            maxLength={150}
            placeholder="e.g. DemoTech"
            onChange={handleChange}
          />
        </label>

        <label className={styles.field}>
          <span>Minimum price</span>
          <input
            name="minPrice"
            type="number"
            value={filters.minPrice}
            min="0"
            step="0.01"
            inputMode="decimal"
            placeholder="0"
            onChange={handleChange}
          />
        </label>

        <label className={styles.field}>
          <span>Maximum price</span>
          <input
            name="maxPrice"
            type="number"
            value={filters.maxPrice}
            min="0"
            step="0.01"
            inputMode="decimal"
            placeholder="Any"
            onChange={handleChange}
          />
        </label>

        <label className={styles.field}>
          <span>Sort by</span>
          <select
            name="sort"
            value={filters.sort}
            onChange={handleChange}
          >
            {SORT_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
      </div>

      {error && (
        <p className={styles.error} role="alert">
          {error}
        </p>
      )}

      <button
        className={styles.submitButton}
        type="submit"
        disabled={isPending}
      >
        {isPending ? "Applying…" : "Apply filters"}
      </button>
    </form>
  );
}
