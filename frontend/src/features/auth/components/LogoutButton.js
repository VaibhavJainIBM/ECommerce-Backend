"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";

export default function LogoutButton({
  className,
  onLoggedOut,
}) {
  const router = useRouter();
  const [isLoggingOut, setIsLoggingOut] =
    useState(false);

  async function handleLogout() {
    setIsLoggingOut(true);

    try {
      const response = await fetch("/api/auth/logout", {
        method: "POST",
      });

      if (!response.ok) {
        throw new Error("Logout request failed.");
      }

      onLoggedOut?.();
      router.replace("/");
      router.refresh();
    } catch (error) {
      console.error("Logout failed:", error);
      setIsLoggingOut(false);
    }
  }

  return (
    <button
      className={className}
      type="button"
      disabled={isLoggingOut}
      onClick={handleLogout}
    >
      {isLoggingOut ? "Signing out…" : "Sign out"}
    </button>
  );
}