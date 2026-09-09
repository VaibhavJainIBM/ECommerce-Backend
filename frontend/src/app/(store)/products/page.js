import EmptyCatalog from "@/features/storefront/components/EmptyCatalog/EmptyCatalog";
import ActiveFilters from "@/features/storefront/components/ActiveFilters/ActiveFilters";
import CatalogControls from "@/features/storefront/components/CatalogControls/CatalogControls";
import ListingCard from "@/features/storefront/components/ListingCard/ListingCard";
import StorefrontPagination from "@/features/storefront/components/StorefrontPagination/StorefrontPagination";
import { getStorefrontListings } from "@/features/storefront/api/storefront-api";
import {
  createProductsHref,
  parseStorefrontQuery,
} from "@/features/storefront/utils/storefront-query";
import styles from "./products.module.css";

const PAGE_SIZE = 12;

export const metadata = {
  title: "Products",
  description: "Browse the available product catalog.",
};

export default async function ProductsPage({ searchParams }) {
  const query = await searchParams;
  const filters = parseStorefrontQuery(query);

  const catalog = await getStorefrontListings({
    ...filters,
    pageSize: PAGE_SIZE,
  });

  const productLabel =
    catalog.totalCount === 1 ? "product" : "products";

  return (
    <>
      <section className={styles.introduction}>
        <div className="site-container">
          <p className={styles.eyebrow}>Products</p>
          <h1 className={styles.title}>Available products</h1>
          <p className={styles.description}>
            View current prices, variants, sellers, and stock.
          </p>
        </div>
      </section>

      <section
        className={`site-container ${styles.catalog}`}
        aria-labelledby="catalog-heading"
      >
        <div className={styles.catalogHeader}>
          <div>
            <h2 id="catalog-heading" className={styles.catalogTitle}>
              Product catalog
            </h2>
            <p className={styles.catalogDescription}>
              Search by product, brand, variant, or seller.
            </p>
          </div>

          <p className={styles.productCount}>
            {catalog.totalCount} {productLabel}
          </p>
        </div>

        <CatalogControls
          key={createProductsHref(filters)}
          initialFilters={filters}
        />

        <ActiveFilters filters={filters} />

        {catalog.items.length > 0 ? (
          <>
            <div className={styles.grid}>
              {catalog.items.map((listing) => (
                <ListingCard key={listing.listingId} listing={listing} />
              ))}
            </div>

            <StorefrontPagination
              currentPage={catalog.page}
              totalPages={catalog.totalPages}
              filters={filters}
            />
          </>
        ) : (
          <EmptyCatalog search={filters.search} />
        )}
      </section>
    </>
  );
}
