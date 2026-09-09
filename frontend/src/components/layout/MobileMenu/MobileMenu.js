"use client";

import { useState } from "react";
import Link from "next/link";
import styles from "./MobileMenu.module.css";

export default function MobileMenu() {
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
          <Link className={styles.link} href="/" onClick={closeMenu}>
            Home
          </Link>

          <Link
            className={styles.link}
            href="/products"
            onClick={closeMenu}
          >
            Products
          </Link>
        </nav>
      )}
    </div>
  );
}
