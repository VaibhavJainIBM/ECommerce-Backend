"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import styles from "./AuthForm.module.css";

export default function LoginForm() {
  const router = useRouter();

  const [credentials, setCredentials] = useState({
    email: "",
    password: "",
  });

  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] =
    useState(false);

  function handleChange(event) {
    const { name, value } = event.target;

    setCredentials((current) => ({
      ...current,
      [name]: value,
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();

    setError("");
    setIsSubmitting(true);

    try {
      const response = await fetch("/api/auth/login", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(credentials),
      });

      const result = await response.json().catch(() => null);

      if (!response.ok) {
        setError(
          result?.message ??
            "We could not sign you in. Please try again."
        );
        return;
      }

      router.replace("/");
      router.refresh();
    } catch {
      setError(
        "The authentication service is unavailable. Please try again."
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className={styles.page}>
      <div className={styles.panel}>
        <p className={styles.eyebrow}>Customer account</p>
        <h1 className={styles.title}>Sign in</h1>
        <p className={styles.description}>
          Sign in to manage your cart and orders.
        </p>

        <form
          className={styles.form}
          onSubmit={handleSubmit}
          aria-busy={isSubmitting}
        >
          <label className={styles.field}>
            <span>Email address</span>
            <input
              name="email"
              type="email"
              value={credentials.email}
              placeholder="you@example.com"
              autoComplete="email"
              required
              disabled={isSubmitting}
              onChange={handleChange}
            />
          </label>

          <label className={styles.field}>
            <span>Password</span>
            <input
              name="password"
              type="password"
              value={credentials.password}
              autoComplete="current-password"
              required
              disabled={isSubmitting}
              onChange={handleChange}
            />
          </label>

          {error && (
            <p className={styles.error} role="alert">
              {error}
            </p>
          )}

          <button
            className={styles.submit}
            type="submit"
            disabled={isSubmitting}
          >
            {isSubmitting ? "Signing in…" : "Sign in"}
          </button>
        </form>

        <p className={styles.footer}>
          Don&apos;t have an account?{" "}
          <Link href="/register">Create one</Link>
        </p>
      </div>
    </section>
  );
}