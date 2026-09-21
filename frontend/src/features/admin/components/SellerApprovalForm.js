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

function formatDate(value) {
  if (!value) {
    return "Not recorded";
  }

  return new Intl.DateTimeFormat("en", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export default function SellerApprovalForm() {
  const router = useRouter();
  const [sellerId, setSellerId] = useState("");
  const [seller, setSeller] = useState(null);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();

    const normalizedSellerId = normalizeGuid(sellerId);

    if (!isGuid(normalizedSellerId)) {
      setError("Enter a valid seller ID.");
      setMessage("");
      return;
    }

    setError("");
    setMessage("");
    setIsSubmitting(true);

    try {
      const approvedSeller = await adminRequest(
        `/api/admin/sellers/${encodeURIComponent(normalizedSellerId)}/approve`,
        { method: "POST" }
      );

      setSeller(approvedSeller);
      setSellerId(approvedSeller.sellerId ?? normalizedSellerId);
      setMessage("Seller approved and activated.");
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
        <p className={styles.panelLabel}>Seller lifecycle</p>
        <h2 className={styles.panelTitle}>Approve a reviewed seller</h2>
        <p className={styles.panelDescription}>
          Approval is valid only after the seller has submitted its profile
          for review and reached the UnderReview status.
        </p>
      </header>

      <form
        className={styles.form}
        onSubmit={handleSubmit}
        aria-busy={isSubmitting}
      >
        <label className={styles.field}>
          <span>Seller ID</span>
          <input
            name="sellerId"
            type="text"
            value={sellerId}
            placeholder="00000000-0000-0000-0000-000000000000"
            autoComplete="off"
            spellCheck="false"
            required
            disabled={isSubmitting}
            onChange={(event) => setSellerId(event.target.value)}
          />
          <small className={styles.hint}>
            Copy the ID from the seller onboarding or review handoff.
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
            {isSubmitting ? "Approving…" : "Approve seller"}
          </button>
        </div>
      </form>

      {seller && (
        <article className={styles.result} aria-label="Approved seller">
          <div className={styles.resultHeader}>
            <h3 className={styles.resultTitle}>{seller.displayName}</h3>
            <span className={styles.status}>{seller.status}</span>
          </div>

          <dl className={styles.details}>
            <div>
              <dt>Seller ID</dt>
              <dd className={styles.code}>{seller.sellerId}</dd>
            </div>
            <div>
              <dt>Legal business</dt>
              <dd>{seller.legalBusinessName}</dd>
            </div>
            <div>
              <dt>Approved</dt>
              <dd>{formatDate(seller.approvedAtUtc)}</dd>
            </div>
            <div>
              <dt>Created</dt>
              <dd>{formatDate(seller.createdAtUtc)}</dd>
            </div>
          </dl>
        </article>
      )}
    </section>
  );
}
