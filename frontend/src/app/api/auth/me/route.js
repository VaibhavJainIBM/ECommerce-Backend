import { NextResponse } from "next/server";
import { authenticatedApiFetch } from "@/lib/auth/authenticated-api-fetch";
import { ApiError } from "@/lib/api/api-error";
import {
  AUTH_COOKIE_NAME,
  getAuthCookieOptions,
} from "@/lib/auth/auth-cookie";

function removeAuthCookie(response) {
  response.cookies.set(AUTH_COOKIE_NAME, "", {
    ...getAuthCookieOptions(),
    expires: new Date(0),
    maxAge: 0,
  });

  return response;
}

export async function GET() {
  try {
    const user = await authenticatedApiFetch(
      "/api/auth/me"
    );

    return NextResponse.json(
      {
        authenticated: true,
        user,
      },
      {
        status: 200,
      }
    );
  } catch (error) {
    if (error instanceof ApiError) {
      const response = NextResponse.json(
        {
          authenticated: false,
          user: null,
          message: error.message,
        },
        {
          status: error.status,
        }
      );

      if (error.status === 401) {
        return removeAuthCookie(response);
      }

      return response;
    }

    console.error("Current-user request failed:", error);

    return NextResponse.json(
      {
        authenticated: false,
        user: null,
        message:
          "The account service is currently unavailable.",
      },
      {
        status: 503,
      }
    );
  }
}