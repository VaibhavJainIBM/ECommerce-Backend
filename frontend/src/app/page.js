import Link from "next/link";
import EmptyCatalog from "@/components/storefront/EmptyCatalog";
import ListingCard from "@/components/storefront/ListingCard";
import Pagination from "@/components/storefront/Pagination";
import SearchForm from "@/components/storefront/SearchForm";
import { getStorefrontListings } from "@/lib/api/storefront";
import styles from "./page.module.css";

const PAGE_SIZE = 12;

function getFirstValue(value) {
  return Array.isArray(value) ? value[0] : value;
}

function getValidPage(value) {
  const parsedPage = Number.parseInt(value ?? "1", 10);

  if (!Number.isInteger(parsedPage) || parsedPage < 1) {
    return 1;
  }

  return parsedPage;
}

export default async function StorefrontPage({
  searchParams,
}) {
  const query = await searchParams;

  const searchValue = getFirstValue(query.search);
  const pageValue = getFirstValue(query.page);

  const search =
    typeof searchValue === "string"
      ? searchValue.trim().slice(0, 100)
      : "";

  const page = getValidPage(pageValue);

  const catalog = await getStorefrontListings({
    search,
    page,
    pageSize: PAGE_SIZE,
  });

  const productLabel =
    catalog.totalCount === 1 ? "product" : "products";

  return (
    <main className={styles.main}>
      <header className={styles.masthead}>
        <div className={styles.container}>
          <nav
            className={styles.navigation}
            aria-label="Primary navigation"
          >
            <Link href="/" className={styles.wordmark}>
              THE MARKET
            </Link>

            <p className={styles.navigationLabel}>
              Curated marketplace
            </p>
          </nav>

          <div className={styles.hero}>
            <p className={styles.eyebrow}>
              Independent sellers
            </p>

            <h1 className={styles.title}>
              Considered goods for everyday living.
            </h1>

            <p className={styles.subtitle}>
              Discover thoughtfully selected products from trusted
              independent sellers.
            </p>
          </div>
        </div>
      </header>

      <section
        id="products"
        className={`${styles.container} ${styles.catalog}`}
        aria-labelledby="products-heading"
      >
        <div className={styles.catalogHeader}>
          <div>
            <p className={styles.catalogLabel}>
              The collection
            </p>

            <h2
              id="products-heading"
              className={styles.catalogTitle}
            >
              Available products
            </h2>

            <p className={styles.catalogDescription}>
              Carefully selected from our marketplace.
            </p>
          </div>

          <p className={styles.productCount}>
            {catalog.totalCount} {productLabel}
          </p>
        </div>

        <SearchForm
          key={search}
          initialSearch={search}
        />

        {catalog.items.length > 0 ? (
          <>
            <div className={styles.grid}>
              {catalog.items.map((listing) => (
                <ListingCard
                  key={listing.listingId}
                  listing={listing}
                />
              ))}
            </div>

            <Pagination
              currentPage={catalog.page}
              totalPages={catalog.totalPages}
              search={search}
            />
          </>
        ) : (
          <EmptyCatalog search={search} />
        )}
      </section>
    </main>
  );
}