"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { jsonRequest, sellerRequest } from "@/features/seller/api/seller-client";
import {
  sellerStatusMessage,
  titleCaseStatus,
} from "@/features/seller/utils/seller-access";
import styles from "./SellerPortal.module.css";

const INITIAL_SELLER = { displayName: "", legalBusinessName: "" };

function formatDate(value) {
  if (!value) return "Not yet";
  return new Intl.DateTimeFormat("en-IN", {
    dateStyle: "medium",
    timeZone: "UTC",
  }).format(new Date(value));
}

export default function SellerStart({ sellers, invitations }) {
  const router = useRouter();
  const [form, setForm] = useState(INITIAL_SELLER);
  const [pending, setPending] = useState("");
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  function change(event) {
    setForm((current) => ({
      ...current,
      [event.target.name]: event.target.value,
    }));
  }

  function handleError(requestError) {
    if (requestError.status === 401) {
      router.replace("/login?next=/seller");
      return;
    }
    setError(requestError.message);
  }

  async function createSeller(event) {
    event.preventDefault();
    setPending("create");
    setError("");
    setMessage("");

    try {
      const seller = await sellerRequest(
        "/api/seller/onboarding",
        jsonRequest("POST", form)
      );
      setForm(INITIAL_SELLER);
      router.push(`/seller/${seller.sellerId}`);
      router.refresh();
    } catch (requestError) {
      handleError(requestError);
    } finally {
      setPending("");
    }
  }

  async function acceptInvitation(sellerId) {
    setPending(`accept:${sellerId}`);
    setError("");
    setMessage("");

    try {
      await sellerRequest(`/api/seller/invitations/${sellerId}/accept`, {
        method: "POST",
      });
      setMessage("Invitation accepted. Your seller workspace is ready.");
      router.refresh();
    } catch (requestError) {
      handleError(requestError);
    } finally {
      setPending("");
    }
  }

  return (
    <div className={styles.sections}>
      {(error || message) && (
        <p
          className={error ? styles.error : styles.success}
          role={error ? "alert" : "status"}
        >
          {error || message}
        </p>
      )}

      {invitations.length > 0 && (
        <section className={styles.panel}>
          <p className={styles.sectionLabel}>Pending invitations</p>
          <h2 className={styles.sectionTitle}>Join a seller team.</h2>
          <div className={styles.list}>
            {invitations.map((invitation) => (
              <article className={styles.card} key={invitation.memberId}>
                <div className={styles.cardHeader}>
                  <h3 className={styles.cardTitle}>{invitation.sellerName}</h3>
                  <div className={styles.roles}>
                    {invitation.roles.map((role) => (
                      <span className={styles.role} key={role}>{role}</span>
                    ))}
                  </div>
                </div>
                <button
                  className={styles.button}
                  type="button"
                  disabled={Boolean(pending)}
                  onClick={() => acceptInvitation(invitation.sellerId)}
                >
                  {pending === `accept:${invitation.sellerId}`
                    ? "Accepting…"
                    : "Accept invitation"}
                </button>
              </article>
            ))}
          </div>
        </section>
      )}

      <div className={styles.twoColumn}>
        <section className={styles.panel}>
          <p className={styles.sectionLabel}>Your seller accounts</p>
          <h2 className={styles.sectionTitle}>Choose a workspace.</h2>

          {sellers.length === 0 ? (
            <p className={styles.cardCopy}>
              You do not have an active seller workspace yet.
            </p>
          ) : (
            <div className={styles.cards}>
              {sellers.map((seller) => {
                const isActiveMember = seller.memberStatus === "Active";
                return (
                  <article className={styles.card} key={seller.sellerId}>
                    <div className={styles.cardHeader}>
                      <div>
                        <h3 className={styles.cardTitle}>{seller.displayName}</h3>
                        <p className={styles.cardCopy}>{seller.legalBusinessName}</p>
                      </div>
                      <span
                        className={styles.status}
                        data-tone={seller.sellerStatus === "Rejected" ? "warning" : "default"}
                      >
                        {titleCaseStatus(seller.sellerStatus)}
                      </span>
                    </div>
                    <p className={styles.cardCopy}>
                      {sellerStatusMessage(seller.sellerStatus)}
                    </p>
                    <div className={styles.roles}>
                      {seller.roles.map((role) => (
                        <span className={styles.role} key={role}>{role}</span>
                      ))}
                      <span className={styles.role}>
                        {titleCaseStatus(seller.memberStatus)} member
                      </span>
                    </div>
                    <div className={styles.metaGrid}>
                      <div>
                        <span className={styles.metaLabel}>Created</span>
                        <span className={styles.metaValue}>
                          {formatDate(seller.sellerCreatedAtUtc)}
                        </span>
                      </div>
                    </div>
                    {isActiveMember && (
                      <Link
                        className={styles.button}
                        href={`/seller/${seller.sellerId}`}
                      >
                        Open workspace
                      </Link>
                    )}
                  </article>
                );
              })}
            </div>
          )}
        </section>

        <section className={styles.panel}>
          <p className={styles.sectionLabel}>Seller onboarding</p>
          <h2 className={styles.sectionTitle}>Open a new shop.</h2>
          <p className={styles.cardCopy}>
            Create the seller record first. An Owner membership is added automatically.
          </p>

          <form className={styles.form} onSubmit={createSeller}>
            <label className={styles.field}>
              <span className={styles.fieldLabel}>Store display name</span>
              <input
                name="displayName"
                value={form.displayName}
                maxLength="150"
                required
                disabled={pending === "create"}
                onChange={change}
              />
            </label>
            <label className={styles.field}>
              <span className={styles.fieldLabel}>Legal business name</span>
              <input
                name="legalBusinessName"
                value={form.legalBusinessName}
                maxLength="250"
                required
                disabled={pending === "create"}
                onChange={change}
              />
            </label>
            <button
              className={styles.button}
              type="submit"
              disabled={Boolean(pending)}
            >
              {pending === "create" ? "Creating…" : "Create seller"}
            </button>
          </form>
        </section>
      </div>
    </div>
  );
}
