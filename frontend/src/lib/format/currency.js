export function formatCurrency(
  amount,
  currencyCode = "INR",
  locale = "en-IN"
) {
  return new Intl.NumberFormat(locale, {
    style: "currency",
    currency: currencyCode,
  }).format(amount);
}