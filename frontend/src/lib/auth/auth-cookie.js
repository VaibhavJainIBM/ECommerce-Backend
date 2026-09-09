export const AUTH_COOKIE_NAME = "ecommerce_access_token";

export function getAuthCookieOptions(expiresAtUtc) {
  const options = {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    priority: "high",
  };

  if (expiresAtUtc) {
    const expires = new Date(expiresAtUtc);

    if (!Number.isNaN(expires.getTime())) {
      options.expires = expires;
    }
  }

  return options;
}
