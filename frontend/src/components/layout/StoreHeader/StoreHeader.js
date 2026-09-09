import Link from "next/link";
import MobileMenu from "../MobileMenu/MobileMenu";
import LogoutButton from "@/features/auth/components/LogoutButton";
import {
  getCurrentUser,
  getUserDisplayName,
} from "@/features/auth/api/get-current-user";
import styles from "./StoreHeader.module.css";

export default async function StoreHeader() {
  const user = await getCurrentUser();
  const displayName = getUserDisplayName(user);
  const isPlatformAdmin = Boolean(
    user?.platformRoles?.some(
      (role) => role.toLowerCase() === "platformadmin"
    )
  );

  return (
    <header className={styles.header}>
      <div className={`site-container ${styles.inner}`}>
        <nav
          className={styles.desktopNavigation}
          aria-label="Primary navigation"
        >
          <Link className={styles.navigationLink} href="/">
            Home
          </Link>

          <Link
            className={styles.navigationLink}
            href="/products"
          >
            Products
          </Link>

          {user && (
            <>
              <Link
                className={styles.navigationLink}
                href="/cart"
              >
                Cart
              </Link>

              <Link
                className={styles.navigationLink}
                href="/orders"
              >
                Orders
              </Link>

              <Link
                className={styles.navigationLink}
                href="/seller"
              >
                Seller
              </Link>
            </>
          )}

          {isPlatformAdmin && (
            <Link
              className={styles.navigationLink}
              href="/admin"
            >
              Admin
            </Link>
          )}
        </nav>

        <div className={styles.desktopAccount}>
          {user ? (
            <>
              <Link
                className={styles.accountLink}
                href="/account"
              >
                {displayName}
              </Link>

              <LogoutButton
                className={styles.logoutButton}
              />
            </>
          ) : (
            <>
              <Link
                className={styles.authLink}
                href="/login"
              >
                Sign in
              </Link>

              <Link
                className={styles.registerLink}
                href="/register"
              >
                Create account
              </Link>
            </>
          )}
        </div>

        <MobileMenu
          isAuthenticated={Boolean(user)}
          displayName={displayName}
          isPlatformAdmin={isPlatformAdmin}
        />
      </div>
    </header>
  );
}
