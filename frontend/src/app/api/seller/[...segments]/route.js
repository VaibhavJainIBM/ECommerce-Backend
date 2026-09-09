import { proxySellerRequest } from "@/features/seller/api/seller-route-proxy";

export function GET(request, context) {
  return proxySellerRequest(request, context, "GET");
}

export function POST(request, context) {
  return proxySellerRequest(request, context, "POST");
}

export function PUT(request, context) {
  return proxySellerRequest(request, context, "PUT");
}

export function PATCH(request, context) {
  return proxySellerRequest(request, context, "PATCH");
}

export function DELETE(request, context) {
  return proxySellerRequest(request, context, "DELETE");
}
