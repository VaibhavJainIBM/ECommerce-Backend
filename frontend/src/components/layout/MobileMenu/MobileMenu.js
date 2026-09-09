"use client";

import { useState } from "react";
import Link from "next/link";
import LogoutButton from "@/features/auth/components/LogoutButton";
import styles from "./MobileMenu.module.css";

export default function MobileMenu({
  isAuthenticated,
  displayName,
  isPlatformAdmin = false,
}) {
  const [isOpen, setIsOpen] = useState(false);

  function toggleMenu() {
    setIsOpen((currentValue) => !currentValue);
  }

  function closeMenu() {
    setIsOpen(false);
  }

  return (
    <div className={styles.mobileMenu}>
      <button
        className={styles.toggle}
        type="button"
        aria-expanded={isOpen}
        aria-controls="mobile-navigation"
        onClick={toggleMenu}
      >
        {isOpen ? "Close" : "Menu"}
      </button>

      {isOpen && (
        <nav
          id="mobile-navigation"
          className={styles.navigation}
          aria-label="Mobile navigation"
        >
          <Link
            className={styles.link}
            href="/"
            onClick={closeMenu}
          >
            Home
          </Link>

          <Link
            className={styles.link}
            href="/products"
            onClick={closeMenu}
          >
            Products
          </Link>

          {isAuthenticated && (
            <>
              <Link
                className={styles.link}
                href="/cart"
                onClick={closeMenu}
              >
                Cart
              </Link>

              <Link
                className={styles.link}
                href="/orders"
                onClick={closeMenu}
              >
                Orders
              </Link>

              <Link
                className={styles.link}
                href="/seller"
                onClick={closeMenu}
              >
                Seller
              </Link>
            </>
          )}

          {isPlatformAdmin && (
            <Link
              className={styles.link}
              href="/admin"
              onClick={closeMenu}
            >
              Admin
            </Link>
          )}

          <div className={styles.accountSection}>
            {isAuthenticated ? (
              <>
                <Link
                  className={styles.link}
                  href="/account"
                  onClick={closeMenu}
                >
                  {displayName || "Account"}
                </Link>

                <LogoutButton
                  className={styles.logoutButton}
                  onLoggedOut={closeMenu}
                />
              </>
            ) : (
              <>
                <Link
                  className={styles.link}
                  href="/login"
                  onClick={closeMenu}
                >
                  Sign in
                </Link>

                <Link
                  className={styles.registerLink}
                  href="/register"
                  onClick={closeMenu}
                >
                  Create account
                </Link>
              </>
            )}
          </div>
        </nav>
      )}
    </div>
  );
}
