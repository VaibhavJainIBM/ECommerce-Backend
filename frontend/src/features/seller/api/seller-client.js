export async function sellerRequest(path, options = {}) {
  const response = await fetch(path, {
    ...options,
    headers: {
      Accept: "application/json",
      ...options.headers,
    },
  });
  const result = await response.json().catch(() => null);

  if (!response.ok) {
    const error = new Error(
      result?.message ?? "The seller request could not be completed."
    );
    error.status = response.status;
    error.code = result?.code ?? null;
    throw error;
  }

  return result;
}

export function jsonRequest(method, body) {
  return {
    method,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  };
}
