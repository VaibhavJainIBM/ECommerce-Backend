import Link from "next/link";
import { redirect } from "next/navigation";
import LogoutButton from "@/features/auth/components/LogoutButton";
import { getAdminProfile } from "@/features/admin/api/admin-api";
import { ApiError } from "@/lib/api/api-error";
import styles from "./layout.module.css";

export const metadata = {
  title: {
    default: "Platform administration",
    template: "%s | Platform administration",
  },
  description: "Manage marketplace approvals and the shared catalog.",
};

async function resolveAdminProfile() {
  try {
    return await getAdminProfile();
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      redirect("/login");
    }

    if (error instanceof ApiError && error.status === 403) {
      return null;
    }

    throw error;
  }
}

export default async function AdminLayout({ children }) {
  const profile = await resolveAdminProfile();

  if (!profile) {
    return (
      <div className={styles.deniedShell}>
        <main id="main-content" className={styles.denied}>
          <p className={styles.deniedLabel}>Restricted workspace</p>
          <h1>Platform administrator access is required.</h1>
          <p>
            This account is authenticated, but it does not have the
            PlatformAdmin role.
          </p>
          <Link href="/">Return to storefront</Link>
        </main>
      </div>
    );
  }

  return (
    <div className={styles.shell}>
      <header className={styles.header}>
        <div className={`site-container ${styles.headerInner}`}>
          <Link className={styles.brand} href="/admin">
            <span className={styles.brandMark}>E</span>
            <span>
              Platform desk
              <small>Administration</small>
            </span>
          </Link>

          <nav className={styles.navigation} aria-label="Admin navigation">
            <Link className={styles.navLink} href="/admin">
              Overview
            </Link>
            <Link className={styles.navLink} href="/admin/sellers">
              Sellers
            </Link>
            <Link className={styles.navLink} href="/admin/listings">
              Listings
            </Link>
            <Link className={styles.navLink} href="/admin/catalog">
              Catalog
            </Link>
          </nav>

          <div className={styles.account}>
            <span className={styles.email}>{profile.email}</span>
            <LogoutButton className={styles.signOut} />
          </div>
        </div>
      </header>

      <main id="main-content" className={styles.main}>
        {children}
      </main>

      <footer className={styles.footer}>
        <div className={`site-container ${styles.footerInner}`}>
          <span>Platform administration</span>
          <Link href="/">View storefront</Link>
        </div>
      </footer>
    </div>
  );
}
