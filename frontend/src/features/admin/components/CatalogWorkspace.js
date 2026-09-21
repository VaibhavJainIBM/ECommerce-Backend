"use client";

import { useRef, useState } from "react";
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

const EMPTY_PRODUCT = {
  title: "",
  brandName: "",
  description: "",
};

function createVariant(clientId) {
  return {
    clientId,
    name: "",
    variantCode: "",
    gtin: "",
  };
}

function validateProduct(product, variants) {
  if (!product.title.trim()) {
    return "Product title is required.";
  }

  if (product.title.trim().length > 250) {
    return "Product title cannot exceed 250 characters.";
  }

  if (!product.brandName.trim()) {
    return "Brand name is required.";
  }

  if (product.brandName.trim().length > 150) {
    return "Brand name cannot exceed 150 characters.";
  }

  if (product.description.trim().length > 4000) {
    return "Product description cannot exceed 4000 characters.";
  }

  if (variants.length === 0) {
    return "Add at least one product variant.";
  }

  const variantCodes = new Set();
  const gtins = new Set();

  for (let index = 0; index < variants.length; index += 1) {
    const variant = variants[index];
    const number = index + 1;
    const name = variant.name.trim();
    const code = variant.variantCode.trim();
    const normalizedCode = code.toUpperCase();
    const normalizedGtin = variant.gtin.replace(/[\s-]/g, "");

    if (!name) {
      return `Variant ${number} name is required.`;
    }

    if (name.length > 150) {
      return `Variant ${number} name cannot exceed 150 characters.`;
    }

    if (!code) {
      return `Variant ${number} code is required.`;
    }

    if (code.length > 64 || !/^[A-Za-z0-9._-]+$/.test(code)) {
      return `Variant ${number} code may contain up to 64 letters, numbers, periods, underscores, and hyphens.`;
    }

    if (variantCodes.has(normalizedCode)) {
      return `Variant code ${code} appears more than once.`;
    }

    variantCodes.add(normalizedCode);

    if (normalizedGtin) {
      if (!/^(?:\d{8}|\d{12}|\d{13}|\d{14})$/.test(normalizedGtin)) {
        return `Variant ${number} GTIN must contain 8, 12, 13, or 14 digits.`;
      }

      if (gtins.has(normalizedGtin)) {
        return `GTIN ${normalizedGtin} appears more than once.`;
      }

      gtins.add(normalizedGtin);
    }
  }

  return "";
}

function ProductResult({ product, isActivating, onActivate }) {
  if (!product) {
    return null;
  }

  return (
    <article className={styles.result} aria-label="Catalog product result">
      <div className={styles.resultHeader}>
        <div>
          <p className={styles.panelLabel}>{product.brandName}</p>
          <h3 className={styles.resultTitle}>{product.title}</h3>
        </div>
        <span className={styles.status}>{product.status}</span>
      </div>

      <dl className={styles.details}>
        <div>
          <dt>Product ID</dt>
          <dd className={styles.code}>{product.productId}</dd>
        </div>
        <div>
          <dt>Variants</dt>
          <dd>{product.variants?.length ?? 0}</dd>
        </div>
      </dl>

      {Array.isArray(product.variants) && product.variants.length > 0 && (
        <div className={styles.tableWrap}>
          <table className={styles.table}>
            <thead>
              <tr>
                <th>Name</th>
                <th>Code</th>
                <th>GTIN</th>
                <th>Status</th>
                <th>Variant ID</th>
              </tr>
            </thead>
            <tbody>
              {product.variants.map((variant) => (
                <tr key={variant.variantId}>
                  <td>{variant.name}</td>
                  <td className={styles.code}>{variant.variantCode}</td>
                  <td className={styles.code}>{variant.gtin || "—"}</td>
                  <td>{variant.status}</td>
                  <td className={styles.code}>{variant.variantId}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {product.status === "Draft" && onActivate && (
        <div className={styles.actions}>
          <button
            className={styles.primaryButton}
            type="button"
            disabled={isActivating}
            onClick={() => onActivate(product.productId)}
          >
            {isActivating ? "Activating…" : "Activate product"}
          </button>
        </div>
      )}
    </article>
  );
}

export default function CatalogWorkspace() {
  const router = useRouter();
  const nextVariantId = useRef(2);
  const [product, setProduct] = useState(EMPTY_PRODUCT);
  const [variants, setVariants] = useState([createVariant("variant-1")]);
  const [activateAfterCreate, setActivateAfterCreate] = useState(false);
  const [createdProduct, setCreatedProduct] = useState(null);
  const [productMessage, setProductMessage] = useState("");
  const [productError, setProductError] = useState("");
  const [isCreating, setIsCreating] = useState(false);
  const [activatingId, setActivatingId] = useState("");

  const [productId, setProductId] = useState("");
  const [activationResult, setActivationResult] = useState(null);
  const [activationMessage, setActivationMessage] = useState("");
  const [activationError, setActivationError] = useState("");

  const [importResult, setImportResult] = useState(null);
  const [importMessage, setImportMessage] = useState("");
  const [importError, setImportError] = useState("");
  const [isImporting, setIsImporting] = useState(false);

  function redirectAfterSessionExpiry(error) {
    if (error.status !== 401) {
      return false;
    }

    router.replace("/login");
    router.refresh();
    return true;
  }

  function updateProduct(event) {
    const { name, value } = event.target;

    setProduct((current) => ({
      ...current,
      [name]: value,
    }));
  }

  function updateVariant(clientId, event) {
    const { name, value } = event.target;

    setVariants((current) =>
      current.map((variant) =>
        variant.clientId === clientId
          ? { ...variant, [name]: value }
          : variant
      )
    );
  }

  function addVariant() {
    if (variants.length >= 100) {
      setProductError("A product cannot contain more than 100 variants.");
      return;
    }

    const clientId = `variant-${nextVariantId.current}`;
    nextVariantId.current += 1;
    setVariants((current) => [...current, createVariant(clientId)]);
    setProductError("");
  }

  function removeVariant(clientId) {
    if (variants.length === 1) {
      setProductError("A product must contain at least one variant.");
      return;
    }

    setVariants((current) =>
      current.filter((variant) => variant.clientId !== clientId)
    );
    setProductError("");
  }

  async function activateCatalogProduct(id) {
    const normalizedProductId = normalizeGuid(id);

    if (!isGuid(normalizedProductId)) {
      throw new Error("Enter a valid product ID.");
    }

    setActivatingId(normalizedProductId);

    try {
      return await adminRequest(
        `/api/admin/catalog/products/${encodeURIComponent(normalizedProductId)}/activate`,
        { method: "POST" }
      );
    } finally {
      setActivatingId("");
    }
  }

  async function handleCreatedProductActivation(id) {
    setProductError("");
    setProductMessage("");

    try {
      const activated = await activateCatalogProduct(id);
      setCreatedProduct(activated);
      setProductMessage("Product and all variants are active.");
    } catch (error) {
      if (!redirectAfterSessionExpiry(error)) {
        setProductError(getAdminErrorMessage(error));
      }
    }
  }

  async function handleCreateProduct(event) {
    event.preventDefault();

    const validationMessage = validateProduct(product, variants);

    if (validationMessage) {
      setProductError(validationMessage);
      setProductMessage("");
      return;
    }

    setProductError("");
    setProductMessage("");
    setIsCreating(true);

    try {
      const created = await adminRequest("/api/admin/catalog/products", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          title: product.title.trim(),
          brandName: product.brandName.trim(),
          description: product.description.trim() || null,
          variants: variants.map((variant) => ({
            name: variant.name.trim(),
            variantCode: variant.variantCode.trim(),
            gtin: variant.gtin.trim() || null,
          })),
        }),
      });

      setCreatedProduct(created);

      if (activateAfterCreate) {
        try {
          const activated = await activateCatalogProduct(created.productId);
          setCreatedProduct(activated);
          setProductMessage("Product created and activated.");
        } catch (activationFailure) {
          if (!redirectAfterSessionExpiry(activationFailure)) {
            setProductError(
              `Product was created as a draft. ${getAdminErrorMessage(activationFailure)}`
            );
          }
        }
      } else {
        setProductMessage("Product created as a draft.");
      }
    } catch (error) {
      if (!redirectAfterSessionExpiry(error)) {
        setProductError(getAdminErrorMessage(error));
      }
    } finally {
      setIsCreating(false);
    }
  }

  async function handleManualActivation(event) {
    event.preventDefault();
    const normalizedProductId = normalizeGuid(productId);

    if (!isGuid(normalizedProductId)) {
      setActivationError("Enter a valid product ID.");
      setActivationMessage("");
      return;
    }

    setActivationError("");
    setActivationMessage("");

    try {
      const activated = await activateCatalogProduct(normalizedProductId);
      setActivationResult(activated);
      setProductId(activated.productId ?? normalizedProductId);
      setActivationMessage("Product and all variants are active.");
    } catch (error) {
      if (!redirectAfterSessionExpiry(error)) {
        setActivationError(getAdminErrorMessage(error));
      }
    }
  }

  async function handleImport(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const fileInput = form.elements.namedItem("file");
    const activateInput = form.elements.namedItem("activate");
    const file = fileInput?.files?.[0];

    if (!file) {
      setImportError("Choose a CSV file to import.");
      setImportMessage("");
      return;
    }

    if (!file.name.toLowerCase().endsWith(".csv")) {
      setImportError("Choose a file with the .csv extension.");
      setImportMessage("");
      return;
    }

    if (file.size > 2 * 1024 * 1024) {
      setImportError("The CSV file cannot exceed 2 MB.");
      setImportMessage("");
      return;
    }

    const body = new FormData();
    body.set("file", file);
    body.set("activate", String(Boolean(activateInput?.checked)));

    setImportError("");
    setImportMessage("");
    setIsImporting(true);

    try {
      const imported = await adminRequest(
        "/api/admin/catalog/products/import-csv",
        {
          method: "POST",
          body,
        }
      );

      setImportResult(imported);
      setImportMessage(
        imported.activated
          ? "Catalog imported and activated."
          : "Catalog imported as drafts."
      );
      form.reset();
    } catch (error) {
      if (!redirectAfterSessionExpiry(error)) {
        setImportError(getAdminErrorMessage(error));
      }
    } finally {
      setIsImporting(false);
    }
  }

  return (
    <div className={styles.form}>
      <section className={styles.panel}>
        <header className={styles.panelHeading}>
          <p className={styles.panelLabel}>Shared catalog</p>
          <h2 className={styles.panelTitle}>Create a product</h2>
          <p className={styles.panelDescription}>
            Define the shared product once, then add every sellable variant.
            Draft products cannot be used for seller listings until activated.
          </p>
        </header>

        <form
          className={styles.form}
          onSubmit={handleCreateProduct}
          aria-busy={isCreating}
        >
          <div className={styles.fieldGrid}>
            <label className={styles.field}>
              <span>Product title</span>
              <input
                name="title"
                type="text"
                value={product.title}
                maxLength="250"
                required
                disabled={isCreating}
                onChange={updateProduct}
              />
            </label>

            <label className={styles.field}>
              <span>Brand name</span>
              <input
                name="brandName"
                type="text"
                value={product.brandName}
                maxLength="150"
                required
                disabled={isCreating}
                onChange={updateProduct}
              />
            </label>
          </div>

          <label className={styles.field}>
            <span>Description</span>
            <textarea
              name="description"
              value={product.description}
              maxLength="4000"
              disabled={isCreating}
              onChange={updateProduct}
            />
            <small className={styles.hint}>
              Optional, up to 4,000 characters.
            </small>
          </label>

          <div className={styles.variants}>
            {variants.map((variant, index) => (
              <div className={styles.variant} key={variant.clientId}>
                <div className={styles.variantHeader}>
                  <h3 className={styles.variantTitle}>
                    Variant {index + 1}
                  </h3>
                  <button
                    className={styles.removeButton}
                    type="button"
                    disabled={isCreating || variants.length === 1}
                    onClick={() => removeVariant(variant.clientId)}
                  >
                    Remove
                  </button>
                </div>

                <div className={styles.fieldGrid}>
                  <label className={styles.field}>
                    <span>Variant name</span>
                    <input
                      name="name"
                      type="text"
                      value={variant.name}
                      maxLength="150"
                      required
                      disabled={isCreating}
                      onChange={(event) =>
                        updateVariant(variant.clientId, event)
                      }
                    />
                  </label>

                  <label className={styles.field}>
                    <span>Variant code</span>
                    <input
                      name="variantCode"
                      type="text"
                      value={variant.variantCode}
                      maxLength="64"
                      pattern="[A-Za-z0-9._-]+"
                      required
                      disabled={isCreating}
                      onChange={(event) =>
                        updateVariant(variant.clientId, event)
                      }
                    />
                  </label>
                </div>

                <label className={styles.field}>
                  <span>GTIN</span>
                  <input
                    name="gtin"
                    type="text"
                    value={variant.gtin}
                    inputMode="numeric"
                    placeholder="Optional: 8, 12, 13, or 14 digits"
                    disabled={isCreating}
                    onChange={(event) =>
                      updateVariant(variant.clientId, event)
                    }
                  />
                </label>
              </div>
            ))}
          </div>

          <div className={styles.actions}>
            <button
              className={styles.secondaryButton}
              type="button"
              disabled={isCreating || variants.length >= 100}
              onClick={addVariant}
            >
              Add variant
            </button>
          </div>

          <label className={styles.checkbox}>
            <input
              type="checkbox"
              checked={activateAfterCreate}
              disabled={isCreating}
              onChange={(event) =>
                setActivateAfterCreate(event.target.checked)
              }
            />
            <span>
              Activate the product and every variant immediately after
              creation.
            </span>
          </label>

          {productError && (
            <p className={styles.error} role="alert">
              {productError}
            </p>
          )}

          {productMessage && (
            <p className={styles.success} role="status">
              {productMessage}
            </p>
          )}

          <div className={styles.actions}>
            <button
              className={styles.primaryButton}
              type="submit"
              disabled={isCreating || Boolean(activatingId)}
            >
              {isCreating ? "Creating…" : "Create product"}
            </button>
          </div>
        </form>

        <ProductResult
          product={createdProduct}
          isActivating={activatingId === createdProduct?.productId}
          onActivate={handleCreatedProductActivation}
        />
      </section>

      <section className={styles.panel}>
        <header className={styles.panelHeading}>
          <p className={styles.panelLabel}>Publish a draft</p>
          <h2 className={styles.panelTitle}>Activate an existing product</h2>
          <p className={styles.panelDescription}>
            Activation publishes the product and every draft variant as one
            atomic catalog change.
          </p>
        </header>

        <form className={styles.form} onSubmit={handleManualActivation}>
          <label className={styles.field}>
            <span>Product ID</span>
            <input
              type="text"
              value={productId}
              placeholder="00000000-0000-0000-0000-000000000000"
              autoComplete="off"
              spellCheck="false"
              required
              disabled={Boolean(activatingId)}
              onChange={(event) => setProductId(event.target.value)}
            />
          </label>

          {activationError && (
            <p className={styles.error} role="alert">
              {activationError}
            </p>
          )}

          {activationMessage && (
            <p className={styles.success} role="status">
              {activationMessage}
            </p>
          )}

          <div className={styles.actions}>
            <button
              className={styles.primaryButton}
              type="submit"
              disabled={Boolean(activatingId)}
            >
              {activatingId ? "Activating…" : "Activate product"}
            </button>
          </div>
        </form>

        <ProductResult product={activationResult} />
      </section>

      <section className={styles.panel}>
        <header className={styles.panelHeading}>
          <p className={styles.panelLabel}>Bulk operation</p>
          <h2 className={styles.panelTitle}>Import catalog CSV</h2>
          <p className={styles.panelDescription}>
            Import up to 1,000 rows in a UTF-8 CSV file no larger than 2 MB.
            Header names are exact and case-sensitive.
          </p>
        </header>

        <form className={styles.form} onSubmit={handleImport}>
          <label className={styles.field}>
            <span>CSV file</span>
            <input
              name="file"
              type="file"
              accept=".csv,text/csv"
              required
              disabled={isImporting}
            />
          </label>

          <label className={styles.checkbox}>
            <input name="activate" type="checkbox" disabled={isImporting} />
            <span>Activate every imported product and variant.</span>
          </label>

          {importError && (
            <p className={styles.error} role="alert">
              {importError}
            </p>
          )}

          {importMessage && (
            <p className={styles.success} role="status">
              {importMessage}
            </p>
          )}

          <div className={styles.actions}>
            <button
              className={styles.primaryButton}
              type="submit"
              disabled={isImporting}
            >
              {isImporting ? "Importing…" : "Import catalog"}
            </button>
            <a
              className={styles.templateLink}
              href="/api/admin/catalog/products/import-csv"
              download
            >
              Download template
            </a>
          </div>
        </form>

        {importResult && (
          <article className={styles.result} aria-label="Catalog import result">
            <div className={styles.resultHeader}>
              <h3 className={styles.resultTitle}>Import summary</h3>
              <span className={styles.status}>
                {importResult.activated ? "Active" : "Draft"}
              </span>
            </div>

            <div className={styles.summaryGrid}>
              <div className={styles.summaryMetric}>
                <span>Rows</span>
                <strong>{importResult.rowsProcessed}</strong>
              </div>
              <div className={styles.summaryMetric}>
                <span>Products</span>
                <strong>{importResult.productsCreated}</strong>
              </div>
              <div className={styles.summaryMetric}>
                <span>Variants</span>
                <strong>{importResult.variantsCreated}</strong>
              </div>
              <div className={styles.summaryMetric}>
                <span>Published</span>
                <strong>{importResult.activated ? "Yes" : "No"}</strong>
              </div>
            </div>

            <div className={styles.variants}>
              {importResult.products?.map((importedProduct) => (
                <details
                  className={styles.variant}
                  key={importedProduct.productId}
                >
                  <summary className={styles.resultHeader}>
                    <span>{importedProduct.productKey}</span>
                    <span className={styles.status}>
                      {importedProduct.status}
                    </span>
                  </summary>

                  <dl className={styles.details}>
                    <div>
                      <dt>Product ID</dt>
                      <dd className={styles.code}>
                        {importedProduct.productId}
                      </dd>
                    </div>
                  </dl>

                  <div className={styles.tableWrap}>
                    <table className={styles.table}>
                      <thead>
                        <tr>
                          <th>CSV row</th>
                          <th>Name</th>
                          <th>Code</th>
                          <th>GTIN</th>
                          <th>Status</th>
                          <th>Variant ID</th>
                        </tr>
                      </thead>
                      <tbody>
                        {importedProduct.variants.map((variant) => (
                          <tr key={variant.variantId}>
                            <td>{variant.csvRowNumber}</td>
                            <td>{variant.variantName}</td>
                            <td className={styles.code}>
                              {variant.variantCode}
                            </td>
                            <td className={styles.code}>
                              {variant.gtin || "—"}
                            </td>
                            <td>{variant.status}</td>
                            <td className={styles.code}>
                              {variant.variantId}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </details>
              ))}
            </div>
          </article>
        )}
      </section>
    </div>
  );
}
