import "server-only";
import { ApiError } from "./api-error";

const API_BASE_URL =
  process.env.ECOMMERCE_API_BASE_URL ??
  "http://localhost:5167";

async function readProblemDetails(response) {
  try {
    return await response.json();
  } catch {
    return null;
  }
}

function getProblemMessage(problem, status) {
  if (problem?.detail) {
    return problem.detail;
  }

  if (problem?.title) {
    return problem.title;
  }

  return `The API request failed with status ${status}.`;
}

export async function apiFetch(path, options = {}) {
  const url = new URL(path, API_BASE_URL);

  const response = await fetch(url, {
    ...options,
    headers: {
      Accept: "application/json",
      ...options.headers,
    },
  });

  if (!response.ok) {
    const problem = await readProblemDetails(response);
    const message = getProblemMessage(
      problem,
      response.status
    );

    throw new ApiError(
      message,
      response.status,
      problem
    );
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}