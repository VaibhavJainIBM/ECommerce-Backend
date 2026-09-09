import { NextResponse } from "next/server";
import { apiFetch } from "@/lib/api/api-fetch";
import { ApiError } from "@/lib/api/api-error";
import {
  AUTH_COOKIE_NAME,
  getAuthCookieOptions,
} from "@/lib/auth/auth-cookie";

function isValidLoginRequest(value) {
  return (
    value &&
    typeof value.email === "string" &&
    value.email.trim() &&
    typeof value.password === "string" &&
    value.password
  );
}

export async function POST(request) {
  let credentials;

  try {
    credentials = await request.json();
  } catch {
    return NextResponse.json(
      {
        message: "The request body must contain valid JSON.",
      },
      {
        status: 400,
      }
    );
  }

  if (!isValidLoginRequest(credentials)) {
    return NextResponse.json(
      {
        message: "Email and password are required.",
      },
      {
        status: 400,
      }
    );
  }

  try {
    const authentication = await apiFetch("/api/auth/login", {
      method: "POST",
      cache: "no-store",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        email: credentials.email.trim(),
        password: credentials.password,
      }),
    });

    if (!authentication?.accessToken) {
      return NextResponse.json(
        {
          message:
            "The authentication service returned an invalid response.",
        },
        {
          status: 502,
        }
      );
    }

    const response = NextResponse.json(
      {
        authenticated: true,
        user: {
          userId: authentication.userId,
          firstName: authentication.firstName,
          lastName: authentication.lastName,
          email: authentication.email,
          platformRoles: authentication.platformRoles ?? [],
        },
      },
      {
        status: 200,
      }
    );

    response.cookies.set(
      AUTH_COOKIE_NAME,
      authentication.accessToken,
      getAuthCookieOptions(authentication.expiresAtUtc)
    );

    return response;
  } catch (error) {
    if (error instanceof ApiError) {
      return NextResponse.json(
        {
          message: error.message,
        },
        {
          status: error.status,
        }
      );
    }

    console.error("Login failed:", error);

    return NextResponse.json(
      {
        message:
          "The authentication service is currently unavailable.",
      },
      {
        status: 503,
      }
    );
  }
}
