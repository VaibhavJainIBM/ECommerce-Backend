import styles from "./ListingCard.module.css";

export default function ListingCard({ listing }) {
  const isInStock = listing.availableQuantity > 0;

  const formattedPrice = new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency: listing.currencyCode,
  }).format(listing.priceAmount);

  const brandInitial = listing.brandName.charAt(0);

  return (
    <article className={styles.card}>
      <div
        className={styles.imagePlaceholder}
        aria-hidden="true"
      >
        {brandInitial}
      </div>

      <div className={styles.content}>
        <p className={styles.brand}>{listing.brandName}</p>

        <h3 className={styles.title}>
          {listing.productTitle}
        </h3>

        <p className={styles.variant}>
          Variant: {listing.variantName}
        </p>

        <p className={styles.seller}>
          Sold by {listing.sellerDisplayName}
        </p>

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
      </div>
    </article>
  );
}