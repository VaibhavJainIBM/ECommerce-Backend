"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import styles from "./AuthForm.module.css";

const INITIAL_FORM = {
  firstName: "",
  lastName: "",
  email: "",
  password: "",
  confirmPassword: "",
};

export default function RegisterForm() {
  const router = useRouter();

  const [registration, setRegistration] =
    useState(INITIAL_FORM);

  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] =
    useState(false);

  function handleChange(event) {
    const { name, value } = event.target;

    setRegistration((current) => ({
      ...current,
      [name]: value,
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();

    setError("");

    if (
      registration.password !==
      registration.confirmPassword
    ) {
      setError("The passwords do not match.");
      return;
    }

    setIsSubmitting(true);

    try {
      const response = await fetch("/api/auth/register", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          firstName: registration.firstName,
          lastName: registration.lastName,
          email: registration.email,
          password: registration.password,
        }),
      });

      const result = await response.json().catch(() => null);

      if (!response.ok) {
        setError(
          result?.message ??
            "We could not create your account. Please try again."
        );
        return;
      }

      router.replace("/");
      router.refresh();
    } catch {
      setError(
        "The registration service is unavailable. Please try again."
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className={styles.page}>
      <div className={styles.panel}>
        <p className={styles.eyebrow}>Customer account</p>
        <h1 className={styles.title}>Create an account</h1>
        <p className={styles.description}>
          Register to start building your cart and placing orders.
        </p>

        <form
          className={styles.form}
          onSubmit={handleSubmit}
          aria-busy={isSubmitting}
        >
          <div className={styles.nameFields}>
            <label className={styles.field}>
              <span>First name</span>
              <input
                name="firstName"
                type="text"
                value={registration.firstName}
                autoComplete="given-name"
                required
                disabled={isSubmitting}
                onChange={handleChange}
              />
            </label>

            <label className={styles.field}>
              <span>Last name</span>
              <input
                name="lastName"
                type="text"
                value={registration.lastName}
                autoComplete="family-name"
                required
                disabled={isSubmitting}
                onChange={handleChange}
              />
            </label>
          </div>

          <label className={styles.field}>
            <span>Email address</span>
            <input
              name="email"
              type="email"
              value={registration.email}
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
              value={registration.password}
              autoComplete="new-password"
              required
              disabled={isSubmitting}
              onChange={handleChange}
            />
          </label>

          <label className={styles.field}>
            <span>Confirm password</span>
            <input
              name="confirmPassword"
              type="password"
              value={registration.confirmPassword}
              autoComplete="new-password"
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
            {isSubmitting
              ? "Creating account…"
              : "Create account"}
          </button>
        </form>

        <p className={styles.footer}>
          Already have an account?{" "}
          <Link href="/login">Sign in</Link>
        </p>
      </div>
    </section>
  );
}