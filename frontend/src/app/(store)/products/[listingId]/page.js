import { cache } from "react";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getStorefrontListing } from "@/features/storefront/api/storefront-api";
import { ApiError } from "@/lib/api/api-error";
import { formatCurrency } from "@/lib/format/currency";
import styles from "./product-detail.module.css";

const loadListing = cache(async (listingId) => {
  try {
    return await getStorefrontListing(listingId);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }

    throw error;
  }
});

export async function generateMetadata({ params }) {
  const { listingId } = await params;
  const listing = await loadListing(listingId);

  return {
    title: listing.productTitle,
    description:
      listing.description ??
      `${listing.productTitle} from ${listing.sellerDisplayName}.`,
  };
}

export default async function ProductDetailPage({ params }) {
  const { listingId } = await params;
  const listing = await loadListing(listingId);
  const formattedPrice = formatCurrency(
    listing.priceAmount,
    listing.currencyCode
  );
  const isInStock = listing.availableQuantity > 0;

  return (
    <div className="site-container">
      <nav className={styles.breadcrumbs} aria-label="Breadcrumb">
        <Link href="/products">Products</Link>
        <span aria-hidden="true">/</span>
        <span aria-current="page">{listing.productTitle}</span>
      </nav>

      <article className={styles.product}>
        <div className={styles.imagePlaceholder} aria-hidden="true">
          {listing.brandName.charAt(0)}
        </div>

        <div className={styles.content}>
          <p className={styles.brand}>{listing.brandName}</p>
          <h1 className={styles.title}>{listing.productTitle}</h1>
          <p className={styles.price}>{formattedPrice}</p>

          {listing.description && (
            <p className={styles.description}>{listing.description}</p>
          )}

          <dl className={styles.details}>
            <div>
              <dt>Variant</dt>
              <dd>{listing.variantName}</dd>
            </div>
            <div>
              <dt>Seller</dt>
              <dd>{listing.sellerDisplayName}</dd>
            </div>
            <div>
              <dt>Seller SKU</dt>
              <dd>{listing.sellerSku}</dd>
            </div>
            <div>
              <dt>Availability</dt>
              <dd className={isInStock ? styles.inStock : styles.outOfStock}>
                {isInStock
                  ? `${listing.availableQuantity} available`
                  : "Out of stock"}
              </dd>
            </div>
          </dl>

          <Link className={styles.backLink} href="/products">
            ← Back to products
          </Link>
        </div>
      </article>
    </div>
  );
}
