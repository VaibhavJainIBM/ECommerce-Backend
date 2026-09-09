"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { sellerRequest } from "@/features/seller/api/seller-client";
import styles from "./SellerPortal.module.css";

export default function SellerLifecycleAction({ sellerId }) {
  const router = useRouter();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");

  async function submit() {
    setPending(true);
    setError("");

    try {
      await sellerRequest(`/api/seller/${sellerId}/submit-for-review`, {
        method: "POST",
      });
      router.refresh();
    } catch (requestError) {
      if (requestError.status === 401) {
        router.replace(`/login?next=/seller/${sellerId}`);
        return;
      }
      setError(requestError.message);
    } finally {
      setPending(false);
    }
  }

  return (
    <div className={styles.stack}>
      {error && <p className={styles.error} role="alert">{error}</p>}
      <button
        className={styles.button}
        type="button"
        disabled={pending}
        onClick={submit}
      >
        {pending ? "Submitting…" : "Submit for platform review"}
      </button>
    </div>
  );
}
