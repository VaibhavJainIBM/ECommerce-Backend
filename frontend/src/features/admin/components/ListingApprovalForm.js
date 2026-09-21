"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import {
  adminRequest,
  getAdminErrorMessage,
} from "@/features/admin/api/admin-client";
import {
  isGuid,
  normalizeGuid,
} from "@/features/admin/api/admin-validation";
import styles from "./AdminForms.module.css";

function formatMoney(amount, currencyCode) {
  try {
    return new Intl.NumberFormat("en", {
      style: "currency",
      currency: currencyCode,
    }).format(amount);
  } catch {
    return `${amount} ${currencyCode}`;
  }
}

export default function ListingApprovalForm() {
  const router = useRouter();
  const [values, setValues] = useState({
    sellerId: "",
    listingId: "",
    rowVersion: "",
  });
  const [listing, setListing] = useState(null);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  function handleChange(event) {
    const { name, value } = event.target;

    setValues((current) => ({
      ...current,
      [name]: value,
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();

    const sellerId = normalizeGuid(values.sellerId);
    const listingId = normalizeGuid(values.listingId);
    const rowVersion = values.rowVersion.trim();

    if (!isGuid(sellerId)) {
      setError("Enter a valid seller ID.");
      setMessage("");
      return;
    }

    if (!isGuid(listingId)) {
      setError("Enter a valid listing ID.");
      setMessage("");
      return;
    }

    if (!rowVersion) {
      setError("Enter the latest listing row version.");
      setMessage("");
      return;
    }

    setError("");
    setMessage("");
    setIsSubmitting(true);

    try {
      const approvedListing = await adminRequest(
        `/api/admin/sellers/${encodeURIComponent(sellerId)}/listings/${encodeURIComponent(listingId)}/approve`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({ rowVersion }),
        }
      );

      setListing(approvedListing);
      setValues({
        sellerId: approvedListing.sellerId ?? sellerId,
        listingId: approvedListing.listingId ?? listingId,
        rowVersion: approvedListing.rowVersion ?? rowVersion,
      });
      setMessage("Listing approved and published.");
    } catch (requestError) {
      if (requestError.status === 401) {
        router.replace("/login");
        router.refresh();
        return;
      }

      setError(getAdminErrorMessage(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className={styles.panel}>
      <header className={styles.panelHeading}>
        <p className={styles.panelLabel}>Listing moderation</p>
        <h2 className={styles.panelTitle}>Approve a pending listing</h2>
        <p className={styles.panelDescription}>
          Use the identifiers and row version from the seller review handoff.
          The row version prevents approval of stale listing data.
        </p>
      </header>

      <form
        className={styles.form}
        onSubmit={handleSubmit}
        aria-busy={isSubmitting}
      >
        <div className={styles.fieldGrid}>
          <label className={styles.field}>
            <span>Seller ID</span>
            <input
              name="sellerId"
              type="text"
              value={values.sellerId}
              placeholder="00000000-0000-0000-0000-000000000000"
              autoComplete="off"
              spellCheck="false"
              required
              disabled={isSubmitting}
              onChange={handleChange}
            />
          </label>

          <label className={styles.field}>
            <span>Listing ID</span>
            <input
              name="listingId"
              type="text"
              value={values.listingId}
              placeholder="00000000-0000-0000-0000-000000000000"
              autoComplete="off"
              spellCheck="false"
              required
              disabled={isSubmitting}
              onChange={handleChange}
            />
          </label>
        </div>

        <label className={styles.field}>
          <span>Row version</span>
          <input
            name="rowVersion"
            type="text"
            value={values.rowVersion}
            placeholder="Base64-encoded row version"
            autoComplete="off"
            spellCheck="false"
            required
            disabled={isSubmitting}
            onChange={handleChange}
          />
          <small className={styles.hint}>
            Use the latest value returned after the seller submits the listing.
          </small>
        </label>

        {error && (
          <p className={styles.error} role="alert">
            {error}
          </p>
        )}

        {message && (
          <p className={styles.success} role="status">
            {message}
          </p>
        )}

        <div className={styles.actions}>
          <button
            className={styles.primaryButton}
            type="submit"
            disabled={isSubmitting}
          >
            {isSubmitting ? "Approving…" : "Approve listing"}
          </button>
        </div>
      </form>

      {listing && (
        <article className={styles.result} aria-label="Approved listing">
          <div className={styles.resultHeader}>
            <h3 className={styles.resultTitle}>{listing.productTitle}</h3>
            <span className={styles.status}>{listing.status}</span>
          </div>

          <dl className={styles.details}>
            <div>
              <dt>Variant</dt>
              <dd>{listing.variantName}</dd>
            </div>
            <div>
              <dt>Seller SKU</dt>
              <dd>{listing.sellerSku}</dd>
            </div>
            <div>
              <dt>Price</dt>
              <dd>
                {formatMoney(listing.priceAmount, listing.currencyCode)}
              </dd>
            </div>
            <div>
              <dt>Listing ID</dt>
              <dd className={styles.code}>{listing.listingId}</dd>
            </div>
            <div>
              <dt>New row version</dt>
              <dd className={styles.code}>{listing.rowVersion}</dd>
            </div>
          </dl>
        </article>
      )}
    </section>
  );
}
