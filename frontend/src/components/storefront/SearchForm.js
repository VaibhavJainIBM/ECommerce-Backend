"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import styles from "./SearchForm.module.css";

export default function SearchForm({ initialSearch = "" }) {
  const router = useRouter();
  const [search, setSearch] = useState(initialSearch);

  function handleSubmit(event) {
    event.preventDefault();

    const normalizedSearch = search.trim();
    const query = new URLSearchParams();

    if (normalizedSearch) {
      query.set("search", normalizedSearch);
    }

    const queryString = query.toString();

    router.push(queryString ? `/?${queryString}` : "/");
  }

  function handleClear() {
    setSearch("");
    router.push("/");
  }

  return (
    <form
      className={styles.form}
      onSubmit={handleSubmit}
      role="search"
    >
      <label className={styles.label} htmlFor="catalog-search">
        Search the collection
      </label>

      <div className={styles.controls}>
        <input
          id="catalog-search"
          className={styles.input}
          name="search"
          type="search"
          value={search}
          maxLength={100}
          placeholder="Search by product, brand, variant, or seller"
          onChange={(event) => setSearch(event.target.value)}
        />

        <button className={styles.submitButton} type="submit">
          Search
        </button>

        {initialSearch && (
          <button
            className={styles.clearButton}
            type="button"
            onClick={handleClear}
          >
            Clear
          </button>
        )}
      </div>
    </form>
  );
}