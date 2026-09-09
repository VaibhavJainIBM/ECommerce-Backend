import { NextResponse } from "next/server";
import {
  AUTH_COOKIE_NAME,
  getAuthCookieOptions,
} from "@/lib/auth/auth-cookie";

export async function POST() {
  const response = NextResponse.json(
    {
      authenticated: false,
    },
    {
      status: 200,
    }
  );

  response.cookies.set(AUTH_COOKIE_NAME, "", {
    ...getAuthCookieOptions(),
    expires: new Date(0),
    maxAge: 0,
  });

  return response;
}