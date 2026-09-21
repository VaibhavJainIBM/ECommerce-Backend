"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { jsonRequest, sellerRequest } from "@/features/seller/api/seller-client";
import { titleCaseStatus } from "@/features/seller/utils/seller-access";
import styles from "./SellerPortal.module.css";

const INITIAL = {
  name: "",
  code: "",
  line1: "",
  line2: "",
  city: "",
  stateOrProvince: "",
  postalCode: "",
  countryCode: "IN",
};

export default function WarehouseManager({ sellerId, sellerStatus, canManage, warehouses }) {
  const router = useRouter();
  const [form, setForm] = useState(INITIAL);
  const [pending, setPending] = useState("");
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const canCreate = canManage && sellerStatus === "Active";

  function change(event) {
    setForm((current) => ({ ...current, [event.target.name]: event.target.value }));
  }

  async function request(key, path, options, success) {
    setPending(key);
    setError("");
    setMessage("");
    try {
      await sellerRequest(path, options);
      setMessage(success);
      router.refresh();
      return true;
    } catch (requestError) {
      if (requestError.status === 401) {
        router.replace(`/login?next=/seller/${sellerId}/warehouses`);
      } else {
        setError(requestError.message);
      }
      return false;
    } finally {
      setPending("");
    }
  }

  async function create(event) {
    event.preventDefault();
    const created = await request(
      "create",
      `/api/seller/${sellerId}/warehouses`,
      jsonRequest("POST", {
        name: form.name,
        code: form.code,
        address: {
          line1: form.line1,
          line2: form.line2 || null,
          city: form.city,
          stateOrProvince: form.stateOrProvince,
          postalCode: form.postalCode,
          countryCode: form.countryCode.toUpperCase(),
        },
      }),
      "Warehouse draft created."
    );
    if (created) setForm(INITIAL);
  }

  function activate(warehouseId) {
    return request(
      `activate:${warehouseId}`,
      `/api/seller/${sellerId}/warehouses/${warehouseId}/activate`,
      { method: "POST" },
      "Warehouse activated."
    );
  }

  return (
    <div className={styles.sections}>
      {(error || message) && (
        <p className={error ? styles.error : styles.success} role={error ? "alert" : "status"}>{error || message}</p>
      )}

      {canManage && (
        <section className={styles.panel}>
          <p className={styles.sectionLabel}>New warehouse</p>
          <h2 className={styles.sectionTitle}>Create a fulfilment location.</h2>
          {!canCreate ? (
            <p className={styles.warning}>The seller must be Active before a warehouse can be created.</p>
          ) : (
            <form className={styles.form} onSubmit={create}>
              <div className={styles.formGrid}>
                <label className={styles.field}>
                  <span className={styles.fieldLabel}>Warehouse name</span>
                  <input name="name" value={form.name} maxLength="150" required disabled={Boolean(pending)} onChange={change} />
                </label>
                <label className={styles.field}>
                  <span className={styles.fieldLabel}>Code</span>
                  <input name="code" value={form.code} maxLength="50" pattern="[A-Za-z0-9_-]+" required disabled={Boolean(pending)} onChange={change} />
                </label>
                <label className={styles.field}>
                  <span className={styles.fieldLabel}>Address line 1</span>
                  <input name="line1" value={form.line1} maxLength="200" required disabled={Boolean(pending)} onChange={change} />
                </label>
                <label className={styles.field}>
                  <span className={styles.fieldLabel}>Address line 2</span>
                  <input name="line2" value={form.line2} maxLength="200" disabled={Boolean(pending)} onChange={change} />
                </label>
                <label className={styles.field}>
                  <span className={styles.fieldLabel}>City</span>
                  <input name="city" value={form.city} maxLength="100" required disabled={Boolean(pending)} onChange={change} />
                </label>
                <label className={styles.field}>
                  <span className={styles.fieldLabel}>State or province</span>
                  <input name="stateOrProvince" value={form.stateOrProvince} maxLength="100" required disabled={Boolean(pending)} onChange={change} />
                </label>
                <label className={styles.field}>
                  <span className={styles.fieldLabel}>Postal code</span>
                  <input name="postalCode" value={form.postalCode} maxLength="20" required disabled={Boolean(pending)} onChange={change} />
                </label>
                <label className={styles.field}>
                  <span className={styles.fieldLabel}>Country code</span>
                  <input name="countryCode" value={form.countryCode} minLength="2" maxLength="2" pattern="[A-Za-z]{2}" required disabled={Boolean(pending)} onChange={change} />
                </label>
              </div>
              <button className={styles.button} type="submit" disabled={Boolean(pending)}>
                {pending === "create" ? "Creating…" : "Create warehouse"}
              </button>
            </form>
          )}
        </section>
      )}

      <section>
        <p className={styles.sectionLabel}>Locations</p>
        <h2 className={styles.sectionTitle}>{warehouses.length} accessible warehouses.</h2>
        {warehouses.length === 0 ? (
          <div className={styles.empty}>No warehouse is available for this membership.</div>
        ) : (
          <div className={styles.cards}>
            {warehouses.map((warehouse) => (
              <article className={styles.card} key={warehouse.warehouseId}>
                <div className={styles.cardHeader}>
                  <div>
                    <p className={styles.sectionLabel}>{warehouse.code}</p>
                    <h3 className={styles.cardTitle}>{warehouse.name}</h3>
                  </div>
                  <span className={styles.status}>{titleCaseStatus(warehouse.status)}</span>
                </div>
                <p className={styles.cardCopy}>
                  {warehouse.address.line1}
                  {warehouse.address.line2 ? `, ${warehouse.address.line2}` : ""}<br />
                  {warehouse.address.city}, {warehouse.address.stateOrProvince} {warehouse.address.postalCode}<br />
                  {warehouse.address.countryCode}
                </p>
                {canManage && sellerStatus === "Active" && warehouse.status !== "Active" && (
                  <button className={styles.secondaryButton} type="button" disabled={Boolean(pending)} onClick={() => activate(warehouse.warehouseId)}>
                    {pending === `activate:${warehouse.warehouseId}` ? "Activating…" : "Activate warehouse"}
                  </button>
                )}
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
