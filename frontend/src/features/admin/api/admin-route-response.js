import "server-only";

import { NextResponse } from "next/server";
import { ApiError } from "@/lib/api/api-error";

function getValidationMessages(problem) {
  if (!problem?.errors || typeof problem.errors !== "object") {
    return [];
  }

  return Object.values(problem.errors)
    .flatMap((value) => (Array.isArray(value) ? value : [value]))
    .filter((value) => typeof value === "string" && value.trim())
    .map((value) => value.trim());
}

export function adminErrorResponse(error, fallbackMessage) {
  if (error instanceof ApiError) {
    return NextResponse.json(
      {
        message: error.message,
        code: error.problem?.code ?? null,
        errors: getValidationMessages(error.problem),
      },
      { status: error.status }
    );
  }

  console.error(fallbackMessage, error);

  return NextResponse.json(
    {
      message: fallbackMessage,
      code: null,
      errors: [],
    },
    { status: 503 }
  );
}

export function badAdminRequest(message) {
  return NextResponse.json(
    {
      message,
      code: "admin.invalid_request",
      errors: [],
    },
    { status: 400 }
  );
}
