import { NextResponse } from "next/server";
import { authenticatedApiFetch } from "@/lib/auth/authenticated-api-fetch";
import {
  adminErrorResponse,
  badAdminRequest,
} from "@/features/admin/api/admin-route-response";

const MAXIMUM_FILE_SIZE = 2 * 1024 * 1024;
const CSV_TEMPLATE = [
  "ProductKey,Title,BrandName,Description,VariantName,VariantCode,Gtin",
  "linen-shirt,Linen Shirt,Atelier North,Breathable linen shirt,Sand / M,LINEN-SAND-M,1234567890123",
  "linen-shirt,Linen Shirt,Atelier North,Breathable linen shirt,Olive / L,LINEN-OLIVE-L,1234567890130",
].join("\r\n");

export function GET() {
  return new Response(CSV_TEMPLATE, {
    headers: {
      "Content-Disposition":
        'attachment; filename="catalog-import-template.csv"',
      "Content-Type": "text/csv; charset=utf-8",
    },
  });
}

export async function POST(request) {
  let incoming;

  try {
    incoming = await request.formData();
  } catch {
    return badAdminRequest("The request must contain form data.");
  }

  const file = incoming.get("file");
  const activate = incoming.get("activate") === "true";

  if (!(file instanceof File) || file.size === 0) {
    return badAdminRequest("Choose a non-empty CSV file.");
  }

  if (!file.name.toLowerCase().endsWith(".csv")) {
    return badAdminRequest("The catalog import must be a .csv file.");
  }

  if (file.size > MAXIMUM_FILE_SIZE) {
    return NextResponse.json(
      {
        message: "The CSV file cannot exceed 2 MB.",
        code: "catalog_import.file_too_large",
        errors: [],
      },
      { status: 413 }
    );
  }

  const outgoing = new FormData();
  outgoing.set("file", file, file.name);
  outgoing.set("activate", String(activate));

  try {
    const result = await authenticatedApiFetch(
      "/api/admin/catalog/products/import-csv",
      {
        method: "POST",
        body: outgoing,
      }
    );

    return NextResponse.json(result, { status: 201 });
  } catch (error) {
    return adminErrorResponse(error, "We could not import this catalog file.");
  }
}
