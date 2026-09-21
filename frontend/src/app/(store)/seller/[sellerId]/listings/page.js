import Link from "next/link";
import {
  getCatalogProducts,
  getSellerListings,
} from "@/features/seller/api/seller-api";
import { requireSeller } from "@/features/seller/api/require-seller";
import ListingManager from "@/features/seller/components/ListingManager";
import { titleCaseStatus } from "@/features/seller/utils/seller-access";
import styles from "@/features/seller/components/SellerPortal.module.css";

export const metadata = { title: "Seller listings" };

function single(value, fallback = "") {
  return Array.isArray(value) ? value[0] ?? fallback : value ?? fallback;
}

export default async function SellerListingsPage({ params, searchParams }) {
  const { sellerId } = await params;
  const seller = await requireSeller(sellerId, ["Owner", "Manager"]);
  const query = await searchParams;
  const rawPage = Number(single(query.page, "1"));
  const page = Number.isInteger(rawPage) && rawPage > 0 ? rawPage : 1;
  const status = single(query.status);
  const catalogSearch = single(query.catalog);
  const [listingPage, catalogPage] = await Promise.all([
    getSellerListings(sellerId, { page, pageSize: 20, status }),
    getCatalogProducts({ search: catalogSearch, pageSize: 100 }),
  ]);

  function pageHref(nextPage) {
    const values = new URLSearchParams();
    values.set("page", String(nextPage));
    if (status) values.set("status", status);
    if (catalogSearch) values.set("catalog", catalogSearch);
    return `?${values.toString()}`;
  }

  return (
    <main className={styles.page}>
      <header className={styles.pageHeader}>
        <p className={styles.eyebrow}>Catalog operations</p>
        <h1 className={styles.pageTitle}>Listings.</h1>
        <p className={styles.description}>
          Build a draft from an active catalog variant, maintain its price,
          and send it to the platform team for approval.
        </p>
      </header>

      <form className={styles.toolbar}>
        <label className={styles.field}>
          <span className={styles.fieldLabel}>Listing status</span>
          <select name="status" defaultValue={status}>
            <option value="">All statuses</option>
            {["Draft", "PendingReview", "Active", "Paused", "Rejected", "Archived"].map((value) => (
              <option value={value} key={value}>{titleCaseStatus(value)}</option>
            ))}
          </select>
        </label>
        <label className={styles.field}>
          <span className={styles.fieldLabel}>Find catalog variants</span>
          <input name="catalog" defaultValue={catalogSearch} maxLength="100" placeholder="Product or brand" />
        </label>
        <button className={styles.button} type="submit">Apply</button>
        {(status || catalogSearch) && (
          <Link className={styles.secondaryButton} href={`/seller/${sellerId}/listings`}>Clear</Link>
        )}
      </form>

      <ListingManager
        sellerId={sellerId}
        sellerStatus={seller.sellerStatus}
        listings={listingPage.items}
        catalogProducts={catalogPage.items}
      />

      {listingPage.totalPages > 1 && (
        <nav className={styles.pagination} aria-label="Listing pages">
          {listingPage.page > 1 ? <Link href={pageHref(listingPage.page - 1)}>Previous</Link> : <span />}
          <span>Page {listingPage.page} of {listingPage.totalPages}</span>
          {listingPage.page < listingPage.totalPages ? <Link href={pageHref(listingPage.page + 1)}>Next</Link> : <span />}
        </nav>
      )}
    </main>
  );
}
