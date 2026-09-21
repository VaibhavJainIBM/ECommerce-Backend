export async function adminRequest(path, options = {}) {
  const response = await fetch(path, {
    ...options,
    headers: {
      Accept: "application/json",
      ...options.headers,
    },
  });

  const payload = await response.json().catch(() => null);

  if (!response.ok) {
    const error = new Error(
      payload?.message ??
        `The request failed with status ${response.status}.`
    );

    error.status = response.status;
    error.code = payload?.code ?? null;
    error.details = Array.isArray(payload?.errors)
      ? payload.errors
      : [];

    throw error;
  }

  return payload;
}

export function getAdminErrorMessage(error) {
  if (Array.isArray(error?.details) && error.details.length > 0) {
    return error.details.join(" ");
  }

  if (error?.status === 401) {
    return "Your session has expired. Sign in and try again.";
  }

  if (error?.status === 403) {
    return "Platform administrator access is required.";
  }

  return error?.message ?? "The request could not be completed.";
}
