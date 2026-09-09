import { NextResponse } from "next/server";
import { apiFetch } from "@/lib/api/api-fetch";
import { ApiError } from "@/lib/api/api-error";
import {
  AUTH_COOKIE_NAME,
  getAuthCookieOptions,
} from "@/lib/auth/auth-cookie";

function isNonEmptyText(value) {
  return typeof value === "string" && value.trim();
}

function isValidRegistrationRequest(value) {
  return (
    value &&
    isNonEmptyText(value.firstName) &&
    isNonEmptyText(value.lastName) &&
    isNonEmptyText(value.email) &&
    isNonEmptyText(value.password)
  );
}

export async function POST(request) {
  let registration;

  try {
    registration = await request.json();
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

  if (!isValidRegistrationRequest(registration)) {
    return NextResponse.json(
      {
        message:
          "First name, last name, email, and password are required.",
      },
      {
        status: 400,
      }
    );
  }

  try {
    const authentication = await apiFetch(
      "/api/auth/register",
      {
        method: "POST",
        cache: "no-store",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          firstName: registration.firstName.trim(),
          lastName: registration.lastName.trim(),
          email: registration.email.trim(),
          password: registration.password,
        }),
      }
    );

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
        status: 201,
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

    console.error("Registration failed:", error);

    return NextResponse.json(
      {
        message:
          "The registration service is currently unavailable.",
      },
      {
        status: 503,
      }
    );
  }
}
