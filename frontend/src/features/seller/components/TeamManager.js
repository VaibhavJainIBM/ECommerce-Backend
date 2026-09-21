"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { jsonRequest, sellerRequest } from "@/features/seller/api/seller-client";
import { titleCaseStatus } from "@/features/seller/utils/seller-access";
import styles from "./SellerPortal.module.css";

export default function TeamManager({ sellerId, members, roles, warehouses }) {
  const router = useRouter();
  const [pending, setPending] = useState("");
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  async function mutate(key, path, method, success, body) {
    setPending(key);
    setError("");
    setMessage("");

    try {
      const options = body === undefined
        ? { method }
        : jsonRequest(method, body);
      await sellerRequest(path, options);
      setMessage(success);
      router.refresh();
      return true;
    } catch (requestError) {
      if (requestError.status === 401) {
        router.replace(`/login?next=/seller/${sellerId}/team`);
      } else {
        setError(requestError.message);
      }
      return false;
    } finally {
      setPending("");
    }
  }

  async function invite(event) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const created = await mutate(
      "invite",
      `/api/seller/${sellerId}/members`,
      "POST",
      "Team invitation created.",
      { email: String(data.get("email")), role: String(data.get("role")) }
    );
    if (created) event.currentTarget.reset();
  }

  function changeState(memberId, action) {
    const method = action === "remove" ? "DELETE" : "POST";
    const path = action === "remove"
      ? `/api/seller/${sellerId}/members/${memberId}`
      : `/api/seller/${sellerId}/members/${memberId}/${action}`;
    const success = {
      suspend: "Member suspended.",
      reactivate: "Member reactivated.",
      remove: "Member removed.",
    }[action];
    return mutate(`${action}:${memberId}`, path, method, success);
  }

  function setRole(memberId, role, assigned) {
    return mutate(
      `role:${memberId}:${role}`,
      `/api/seller/${sellerId}/members/${memberId}/roles/${encodeURIComponent(role)}`,
      assigned ? "PUT" : "DELETE",
      "Role assignment updated."
    );
  }

  function setWarehouse(memberId, warehouseId, assigned) {
    return mutate(
      `warehouse:${memberId}:${warehouseId}`,
      `/api/seller/${sellerId}/members/${memberId}/warehouses/${warehouseId}`,
      assigned ? "PUT" : "DELETE",
      "Warehouse assignment updated."
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
        <p className={styles.sectionLabel}>Invite a teammate</p>
        <h2 className={styles.sectionTitle}>Extend seller access.</h2>
        <form className={styles.form} onSubmit={invite}>
          <label className={styles.field}>
            <span className={styles.fieldLabel}>Registered email</span>
            <input name="email" type="email" maxLength="256" required />
          </label>
          <label className={styles.field}>
            <span className={styles.fieldLabel}>Initial role</span>
            <select name="role" defaultValue="Manager" required>
              {roles.map((role) => <option key={role.name}>{role.name}</option>)}
            </select>
          </label>
          <button className={styles.button} disabled={Boolean(pending)}>
            {pending === "invite" ? "Inviting…" : "Send invitation"}
          </button>
        </form>
      </section>
      <section>
        <p className={styles.sectionLabel}>Members</p>
        <h2 className={styles.sectionTitle}>{members.length} team members.</h2>
        <div className={styles.list}>
          {members.map((member) => (
            <MemberCard
              key={member.memberId}
              member={member}
              roles={roles}
              warehouses={warehouses}
              pending={pending}
              onRole={setRole}
              onWarehouse={setWarehouse}
              onState={changeState}
            />
          ))}
        </div>
      </section>
    </div>
  );
}

function MemberCard({
  member,
  roles,
  warehouses,
  pending,
  onRole,
  onWarehouse,
  onState,
}) {
  const removed = member.status === "Removed";

  return (
    <article className={styles.card}>
      <div className={styles.cardHeader}>
        <div>
          <h3 className={styles.cardTitle}>{member.email}</h3>
          <p className={styles.cardCopy}>
            Member ID: <span className={styles.mono}>{member.memberId}</span>
          </p>
        </div>
        <span
          className={styles.status}
          data-tone={removed || member.status === "Suspended" ? "warning" : "default"}
        >
          {titleCaseStatus(member.status)}
        </span>
      </div>

      {!removed && (
        <MemberAssignments
          member={member}
          roles={roles}
          warehouses={warehouses}
          pending={pending}
          onRole={onRole}
          onWarehouse={onWarehouse}
        />
      )}

      <MemberStateActions member={member} pending={pending} onState={onState} />
    </article>
  );
}

function MemberAssignments({ member, roles, warehouses, pending, onRole, onWarehouse }) {
  return (
    <div className={styles.detailGrid}>
      <div>
        <p className={styles.metaLabel}>Roles</p>
        <div className={styles.checkboxList}>
          {roles.map((role) => (
            <RoleToggle
              key={role.name}
              member={member}
              role={role}
              pending={pending}
              onRole={onRole}
            />
          ))}
        </div>
      </div>
      <WarehouseToggles
        member={member}
        warehouses={warehouses}
        pending={pending}
        onWarehouse={onWarehouse}
      />
    </div>
  );
}

function MemberStateActions({ member, pending, onState }) {
  if (member.status === "Removed") return null;
  return (
    <div className={styles.actions}>
      {member.status === "Active" && (
        <button className={styles.secondaryButton} type="button" disabled={Boolean(pending)} onClick={() => onState(member.memberId, "suspend")}>
          {pending === `suspend:${member.memberId}` ? "Suspending…" : "Suspend"}
        </button>
      )}
      {member.status === "Suspended" && (
        <button className={styles.secondaryButton} type="button" disabled={Boolean(pending)} onClick={() => onState(member.memberId, "reactivate")}>
          {pending === `reactivate:${member.memberId}` ? "Reactivating…" : "Reactivate"}
        </button>
      )}
      <button className={styles.dangerButton} type="button" disabled={Boolean(pending)} onClick={() => onState(member.memberId, "remove")}>
        {pending === `remove:${member.memberId}` ? "Removing…" : "Remove"}
      </button>
    </div>
  );
}

function RoleToggle({ member, role, pending, onRole }) {
  const checked = member.roles.includes(role.name);
  return (
    <label className={styles.checkbox}>
      <input
        type="checkbox"
        checked={checked}
        disabled={Boolean(pending)}
        onChange={(event) =>
          onRole(member.memberId, role.name, event.target.checked)
        }
      />
      {role.name} — {role.description}
    </label>
  );
}

function WarehouseToggles({ member, warehouses, pending, onWarehouse }) {
  return (
    <div>
      <p className={styles.metaLabel}>Warehouse scope</p>
      {warehouses.length === 0 ? (
        <p className={styles.cardCopy}>No warehouses exist yet.</p>
      ) : (
        <div className={styles.checkboxList}>
          {warehouses.map((warehouse) => (
            <label className={styles.checkbox} key={warehouse.warehouseId}>
              <input
                type="checkbox"
                checked={member.warehouseIds.includes(warehouse.warehouseId)}
                disabled={Boolean(pending)}
                onChange={(event) =>
                  onWarehouse(
                    member.memberId,
                    warehouse.warehouseId,
                    event.target.checked
                  )
                }
              />
              {warehouse.name} ({warehouse.code})
            </label>
          ))}
        </div>
      )}
    </div>
  );
}
