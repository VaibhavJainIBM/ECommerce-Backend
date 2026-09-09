import Link from "next/link";
import { formatCurrency } from "@/lib/format/currency";
import styles from "./ListingCard.module.css";

export default function ListingCard({ listing }) {
  const isInStock = listing.availableQuantity > 0;

  const formattedPrice = formatCurrency(
    listing.priceAmount,
    listing.currencyCode
  );

  const brandInitial = listing.brandName.charAt(0);

  return (
    <article className={styles.card}>
      <Link
        className={styles.imagePlaceholder}
        href={`/products/${listing.listingId}`}
        aria-label={`View ${listing.productTitle}`}
      >
        <span aria-hidden="true">{brandInitial}</span>
      </Link>

      <div className={styles.content}>
        <p className={styles.brand}>{listing.brandName}</p>

        <h3 className={styles.title}>
          <Link href={`/products/${listing.listingId}`}>
            {listing.productTitle}
          </Link>
        </h3>

        <p className={styles.variant}>
          Variant: {listing.variantName}
        </p>

        <p className={styles.seller}>
          Sold by {listing.sellerDisplayName}
        </p>

        {listing.description && (
          <p className={styles.description}>{listing.description}</p>
        )}

        <div className={styles.priceRow}>
          <p className={styles.price}>{formattedPrice}</p>

          <p
            className={`${styles.stock} ${
              isInStock ? styles.inStock : styles.outOfStock
            }`}
          >
            {isInStock ? "In stock" : "Out of stock"}
          </p>
        </div>

        <Link
          className={styles.detailsLink}
          href={`/products/${listing.listingId}`}
        >
          View details
        </Link>
      </div>
    </article>
  );
}
