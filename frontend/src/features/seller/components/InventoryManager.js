"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { jsonRequest, sellerRequest } from "@/features/seller/api/seller-client";
import styles from "./SellerPortal.module.css";

export default function InventoryManager({
  sellerId,
  sellerStatus,
  inventory,
  warehouses,
  listings,
}) {
  const router = useRouter();
  const [pending, setPending] = useState("");
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const activeWarehouses = warehouses.filter((item) => item.status === "Active");
  const usableListings = listings.filter((item) => item.status !== "Archived");

  async function mutate(key, path, body, success) {
    setPending(key);
    setError("");
    setMessage("");
    try {
      await sellerRequest(path, jsonRequest("POST", body));
      setMessage(success);
      router.refresh();
      return true;
    } catch (requestError) {
      if (requestError.status === 401) {
        router.replace(`/login?next=/seller/${sellerId}/inventory`);
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
    const data = new FormData(event.currentTarget);
    const created = await mutate(
      "create",
      `/api/seller/${sellerId}/inventory`,
      {
        warehouseId: String(data.get("warehouseId")),
        sellerListingId: String(data.get("sellerListingId")),
        initialQuantity: Number(data.get("initialQuantity")),
      },
      "Inventory record created."
    );
    if (created) event.currentTarget.reset();
  }

  function update(event, item, action) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const quantity = Number(data.get("quantity"));
    const label = action === "receive" ? "Stock received." : "On-hand quantity adjusted.";
    return mutate(
      `${action}:${item.inventoryItemId}`,
      `/api/seller/${sellerId}/inventory/${item.inventoryItemId}/${action}`,
      { quantity, rowVersion: item.rowVersion },
      label
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
        <p className={styles.sectionLabel}>New inventory record</p>
        <h2 className={styles.sectionTitle}>Connect a listing to a warehouse.</h2>
        {sellerStatus !== "Active" ? (
          <p className={styles.warning}>The seller must be Active before inventory can be created.</p>
        ) : activeWarehouses.length === 0 ? (
          <p className={styles.warning}>Activate an assigned warehouse before adding inventory.</p>
        ) : (
          <form className={styles.form} onSubmit={create}>
            <div className={styles.formGrid}>
              <label className={styles.field}>
                <span className={styles.fieldLabel}>Warehouse</span>
                <select name="warehouseId" required>
                  <option value="">Select a warehouse</option>
                  {activeWarehouses.map((warehouse) => (
                    <option key={warehouse.warehouseId} value={warehouse.warehouseId}>
                      {warehouse.name} ({warehouse.code})
                    </option>
                  ))}
                </select>
              </label>
              <label className={styles.field}>
                <span className={styles.fieldLabel}>Seller listing</span>
                {usableListings.length > 0 ? (
                  <select name="sellerListingId" required>
                    <option value="">Select a listing</option>
                    {usableListings.map((listing) => (
                      <option key={listing.listingId} value={listing.listingId}>
                        {listing.productTitle} — {listing.variantName} ({listing.sellerSku})
                      </option>
                    ))}
                  </select>
                ) : (
                  <input name="sellerListingId" required placeholder="Seller listing ID" />
                )}
              </label>
              <label className={styles.field}>
                <span className={styles.fieldLabel}>Initial on-hand quantity</span>
                <input name="initialQuantity" type="number" min="0" step="1" defaultValue="0" required />
              </label>
            </div>
            <p className={styles.help}>Each warehouse and listing pair can have one inventory record.</p>
            <button className={styles.button} type="submit" disabled={Boolean(pending)}>
              {pending === "create" ? "Creating…" : "Create inventory"}
            </button>
          </form>
        )}
      </section>

      <section>
        <p className={styles.sectionLabel}>Stock ledger</p>
        <h2 className={styles.sectionTitle}>{inventory.length} inventory records.</h2>
        {inventory.length === 0 ? (
          <div className={styles.empty}>No inventory is available for your assigned warehouses.</div>
        ) : (
          <div className={styles.list}>
            {inventory.map((item) => (
              <article className={styles.card} key={item.inventoryItemId}>
                <div className={styles.cardHeader}>
                  <div>
                    <p className={styles.sectionLabel}>{item.warehouseCode}</p>
                    <h3 className={styles.cardTitle}>{item.sellerSku}</h3>
                    <p className={styles.cardCopy}>{item.warehouseName}</p>
                  </div>
                  <span className={styles.mono}>{item.inventoryItemId}</span>
                </div>
                <div className={styles.stats}>
                  <div className={styles.stat}><span className={styles.metaLabel}>On hand</span><strong className={styles.metaValue}>{item.onHandQuantity}</strong></div>
                  <div className={styles.stat}><span className={styles.metaLabel}>Reserved</span><strong className={styles.metaValue}>{item.reservedQuantity}</strong></div>
                  <div className={styles.stat}><span className={styles.metaLabel}>Available</span><strong className={styles.metaValue}>{item.availableQuantity}</strong></div>
                </div>
                <div className={styles.formGrid}>
                  <form className={styles.inlineForm} onSubmit={(event) => update(event, item, "receive")}>
                    <label className={styles.field}>
                      <span className={styles.fieldLabel}>Receive quantity</span>
                      <input name="quantity" type="number" min="1" step="1" required />
                    </label>
                    <button className={styles.button} type="submit" disabled={Boolean(pending)}>
                      {pending === `receive:${item.inventoryItemId}` ? "Receiving…" : "Receive stock"}
                    </button>
                  </form>
                  <form className={styles.inlineForm} onSubmit={(event) => update(event, item, "adjust")}>
                    <label className={styles.field}>
                      <span className={styles.fieldLabel}>Set on-hand quantity</span>
                      <input name="quantity" type="number" min="0" step="1" defaultValue={item.onHandQuantity} required />
                    </label>
                    <button className={styles.secondaryButton} type="submit" disabled={Boolean(pending)}>
                      {pending === `adjust:${item.inventoryItemId}` ? "Adjusting…" : "Adjust total"}
                    </button>
                  </form>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
