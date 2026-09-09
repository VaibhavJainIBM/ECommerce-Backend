import "server-only";

import { NextResponse } from "next/server";
import { ApiError } from "@/lib/api/api-error";
import { authenticatedApiFetch } from "@/lib/auth/authenticated-api-fetch";
import {
  AUTH_COOKIE_NAME,
  getAuthCookieOptions,
} from "@/lib/auth/auth-cookie";

function backendPath(segments) {
  if (segments.length === 1 && segments[0] === "onboarding") {
    return "/api/sellers";
  }

  if (segments.length === 1 && segments[0] === "mine") {
    return "/api/sellers/mine";
  }

  if (segments.length === 1 && segments[0] === "invitations") {
    return "/api/seller-invitations";
  }

  if (
    segments.length === 3 &&
    segments[0] === "invitations" &&
    segments[2] === "accept"
  ) {
    return `/api/sellers/${encodeURIComponent(segments[1])}/invitations/accept`;
  }

  if (segments.length === 1 && segments[0] === "catalog") {
    return "/api/catalog/products";
  }

  return `/api/sellers/${segments.map(encodeURIComponent).join("/")}`;
}

function problemResponse(error) {
  if (error instanceof ApiError) {
    const validationMessages = Object.values(error.problem?.errors ?? {})
      .flat()
      .filter(Boolean);
    const response = NextResponse.json(
      {
        message: validationMessages[0] ?? error.message,
        errors: error.problem?.errors ?? null,
        code: error.problem?.code ?? null,
      },
      { status: error.status }
    );

    if (error.status === 401) {
      response.cookies.set(AUTH_COOKIE_NAME, "", {
        ...getAuthCookieOptions(),
        expires: new Date(0),
        maxAge: 0,
      });
    }

    return response;
  }

  console.error("Seller request failed:", error);
  return NextResponse.json(
    { message: "The seller service is currently unavailable." },
    { status: 503 }
  );
}

export async function proxySellerRequest(request, context, method) {
  const { segments } = await context.params;
  const path = `${backendPath(segments)}${request.nextUrl.search}`;
  const options = { method, cache: "no-store" };

  if (!["GET", "HEAD"].includes(method)) {
    const body = await request.text();
    if (body) {
      options.body = body;
      options.headers = {
        "Content-Type": request.headers.get("content-type") ?? "application/json",
      };
    }
  }

  try {
    const result = await authenticatedApiFetch(path, options);
    return result === null
      ? new NextResponse(null, { status: 204 })
      : NextResponse.json(result);
  } catch (error) {
    return problemResponse(error);
  }
}
