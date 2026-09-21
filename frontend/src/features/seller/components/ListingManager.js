"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { formatCurrency } from "@/lib/format/currency";
import { jsonRequest, sellerRequest } from "@/features/seller/api/seller-client";
import { titleCaseStatus } from "@/features/seller/utils/seller-access";
import styles from "./SellerPortal.module.css";

const INITIAL_LISTING = {
  productVariantId: "",
  sellerSku: "",
  priceAmount: "",
  currencyCode: "INR",
};

export default function ListingManager({
  sellerId,
  sellerStatus,
  listings,
  catalogProducts,
}) {
  const router = useRouter();
  const [form, setForm] = useState(INITIAL_LISTING);
  const [pending, setPending] = useState("");
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const canCreate = ["PendingVerification", "UnderReview", "Rejected", "Active"].includes(sellerStatus);

  function change(event) {
    setForm((current) => ({ ...current, [event.target.name]: event.target.value }));
  }

  function handleError(requestError) {
    if (requestError.status === 401) {
      router.replace(`/login?next=/seller/${sellerId}/listings`);
      return;
    }
    setError(requestError.message);
  }

  async function mutate(key, path, options, successMessage) {
    setPending(key);
    setError("");
    setMessage("");
    try {
      await sellerRequest(path, options);
      setMessage(successMessage);
      router.refresh();
      return true;
    } catch (requestError) {
      handleError(requestError);
      return false;
    } finally {
      setPending("");
    }
  }

  async function createListing(event) {
    event.preventDefault();
    const created = await mutate(
      "create",
      `/api/seller/${sellerId}/listings`,
      jsonRequest("POST", {
        ...form,
        priceAmount: Number(form.priceAmount),
        currencyCode: form.currencyCode.trim().toUpperCase(),
      }),
      "Draft listing created."
    );
    if (created) setForm(INITIAL_LISTING);
  }

  function updatePrice(event, listing) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    return mutate(
      `price:${listing.listingId}`,
      `/api/seller/${sellerId}/listings/${listing.listingId}/price`,
      jsonRequest("PATCH", {
        priceAmount: Number(data.get("priceAmount")),
        currencyCode: String(data.get("currencyCode")).trim().toUpperCase(),
        rowVersion: listing.rowVersion,
      }),
      "Listing price updated."
    );
  }

  function submitForReview(listing) {
    return mutate(
      `review:${listing.listingId}`,
      `/api/seller/${sellerId}/listings/${listing.listingId}/submit-for-review`,
      jsonRequest("POST", { rowVersion: listing.rowVersion }),
      "Listing sent for platform review."
    );
  }

  function archive(listing) {
    return mutate(
      `archive:${listing.listingId}`,
      `/api/seller/${sellerId}/listings/${listing.listingId}/archive`,
      jsonRequest("POST", { rowVersion: listing.rowVersion }),
      "Listing archived."
    );
  }

  return (
    <div className={styles.sections}>
      {(error || message) && (
        <p className={error ? styles.error : styles.success} role={error ? "alert" : "status"}>
          {error || message}
        </p>
      )}

      <section className={styles.panel}>
        <p className={styles.sectionLabel}>New listing</p>
        <h2 className={styles.sectionTitle}>Create a priced draft.</h2>
        {!canCreate ? (
          <p className={styles.warning}>This seller status cannot create listings.</p>
        ) : catalogProducts.length === 0 ? (
          <p className={styles.message}>No active catalog variants are available.</p>
        ) : (
          <form className={styles.form} onSubmit={createListing}>
            <div className={styles.formGrid}>
              <label className={styles.field}>
                <span className={styles.fieldLabel}>Catalog variant</span>
                <select
                  name="productVariantId"
                  value={form.productVariantId}
                  required
                  disabled={Boolean(pending)}
                  onChange={change}
                >
                  <option value="">Select a product variant</option>
                  {catalogProducts.map((product) =>
                    product.variants.map((variant) => (
                      <option key={variant.variantId} value={variant.variantId}>
                        {product.brandName} — {product.title} — {variant.name} ({variant.variantCode})
                      </option>
                    ))
                  )}
                </select>
              </label>
              <label className={styles.field}>
                <span className={styles.fieldLabel}>Seller SKU</span>
                <input
                  name="sellerSku"
                  value={form.sellerSku}
                  maxLength="64"
                  pattern="[A-Za-z0-9._-]+"
                  title="Letters, numbers, hyphens, underscores, and periods only."
                  required
                  disabled={Boolean(pending)}
                  onChange={change}
                />
              </label>
              <label className={styles.field}>
                <span className={styles.fieldLabel}>Price</span>
                <input
                  name="priceAmount"
                  type="number"
                  min="0.01"
                  step="0.01"
                  value={form.priceAmount}
                  required
                  disabled={Boolean(pending)}
                  onChange={change}
                />
              </label>
              <label className={styles.field}>
                <span className={styles.fieldLabel}>Currency</span>
                <input
                  name="currencyCode"
                  value={form.currencyCode}
                  minLength="3"
                  maxLength="3"
                  pattern="[A-Za-z]{3}"
                  required
                  disabled={Boolean(pending)}
                  onChange={change}
                />
              </label>
            </div>
            <button className={styles.button} type="submit" disabled={Boolean(pending)}>
              {pending === "create" ? "Creating…" : "Create draft"}
            </button>
          </form>
        )}
      </section>

      <section>
        <div className={styles.cardHeader}>
          <div>
            <p className={styles.sectionLabel}>Seller listings</p>
            <h2 className={styles.sectionTitle}>{listings.length} on this page.</h2>
          </div>
        </div>

        {listings.length === 0 ? (
          <div className={styles.empty}>No listings match the current filter.</div>
        ) : (
          <div className={styles.list}>
            {listings.map((listing) => {
              const canPrice = ["Draft", "Rejected", "Paused", "Active"].includes(listing.status);
              const canReview = sellerStatus === "Active" && ["Draft", "Rejected"].includes(listing.status);
              const isArchived = listing.status === "Archived";
              return (
                <article className={styles.card} key={listing.listingId}>
                  <div className={styles.cardHeader}>
                    <div>
                      <p className={styles.sectionLabel}>{listing.brandName}</p>
                      <h3 className={styles.cardTitle}>{listing.productTitle}</h3>
                      <p className={styles.cardCopy}>
                        {listing.variantName} · <span className={styles.mono}>{listing.sellerSku}</span>
                      </p>
                    </div>
                    <div className={styles.stack}>
                      <span
                        className={styles.status}
                        data-tone={listing.status === "Rejected" ? "warning" : "default"}
                      >
                        {titleCaseStatus(listing.status)}
                      </span>
                      <span className={styles.price}>
                        {formatCurrency(listing.priceAmount, listing.currencyCode)}
                      </span>
                    </div>
                  </div>

                  {canPrice && (
                    <form className={styles.inlineForm} onSubmit={(event) => updatePrice(event, listing)}>
                      <label className={styles.field}>
                        <span className={styles.fieldLabel}>New price</span>
                        <input name="priceAmount" type="number" min="0.01" step="0.01" defaultValue={listing.priceAmount} required />
                      </label>
                      <label className={styles.field}>
                        <span className={styles.fieldLabel}>Currency</span>
                        <input name="currencyCode" minLength="3" maxLength="3" pattern="[A-Za-z]{3}" defaultValue={listing.currencyCode} required />
                      </label>
                      <button className={styles.secondaryButton} type="submit" disabled={Boolean(pending)}>
                        {pending === `price:${listing.listingId}` ? "Saving…" : "Update price"}
                      </button>
                    </form>
                  )}

                  <div className={styles.actions}>
                    {canReview && (
                      <button className={styles.button} type="button" disabled={Boolean(pending)} onClick={() => submitForReview(listing)}>
                        {pending === `review:${listing.listingId}` ? "Submitting…" : "Submit for review"}
                      </button>
                    )}
                    {!isArchived && (
                      <button className={styles.dangerButton} type="button" disabled={Boolean(pending)} onClick={() => archive(listing)}>
                        {pending === `archive:${listing.listingId}` ? "Archiving…" : "Archive"}
                      </button>
                    )}
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>
    </div>
  );
}
