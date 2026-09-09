import { redirect } from "next/navigation";
import {
  getCurrentUser,
  getUserDisplayName,
} from "@/features/auth/api/get-current-user";
import styles from "./account.module.css";

export const metadata = {
  title: "My account",
  description: "View your customer account.",
};

export default async function AccountPage() {
  const user = await getCurrentUser();

  if (!user) {
    redirect("/login");
  }

  const displayName = getUserDisplayName(user);

  return (
    <section className={`site-container ${styles.page}`}>
      <div className={styles.heading}>
        <p className={styles.eyebrow}>Customer account</p>
        <h1 className={styles.title}>{displayName}</h1>
        <p className={styles.description}>
          View your account information and customer activity.
        </p>
      </div>

      <div className={styles.card}>
        <h2 className={styles.cardTitle}>
          Account information
        </h2>

        <dl className={styles.details}>
          <div>
            <dt>First name</dt>
            <dd>{user.firstName || "Not provided"}</dd>
          </div>

          <div>
            <dt>Last name</dt>
            <dd>{user.lastName || "Not provided"}</dd>
          </div>

          <div>
            <dt>Email address</dt>
            <dd>{user.email}</dd>
          </div>
        </dl>
      </div>
    </section>
  );
}