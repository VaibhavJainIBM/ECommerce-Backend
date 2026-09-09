function getFirstValue(value) {
  return Array.isArray(value) ? value[0] : value;
}

export const DEFAULT_SORT = "name_asc";

export const SORT_OPTIONS = [
  { value: "name_asc", label: "Name: A to Z" },
  { value: "name_desc", label: "Name: Z to A" },
  { value: "price_asc", label: "Price: low to high" },
  { value: "price_desc", label: "Price: high to low" },
];

function getValidPage(value) {
  const parsedPage = Number.parseInt(value ?? "1", 10);

  if (!Number.isInteger(parsedPage) || parsedPage < 1) {
    return 1;
  }

  return parsedPage;
}

function getText(value, maxLength) {
  const firstValue = getFirstValue(value);

  return typeof firstValue === "string"
    ? firstValue.trim().slice(0, maxLength)
    : "";
}

function getMoney(value) {
  const text = getText(value, 20);
  const amount = Number(text);

  return text && Number.isFinite(amount) && amount >= 0
    ? text
    : "";
}

function getSort(value) {
  const candidate = getText(value, 20);

  return SORT_OPTIONS.some((option) => option.value === candidate)
    ? candidate
    : DEFAULT_SORT;
}

export function parseStorefrontQuery(searchParams) {
  const pageValue = getFirstValue(searchParams.page);

  return {
    search: getText(searchParams.search, 100),
    brand: getText(searchParams.brand, 150),
    minPrice: getMoney(searchParams.minPrice),
    maxPrice: getMoney(searchParams.maxPrice),
    sort: getSort(searchParams.sort),
    page: getValidPage(pageValue),
  };
}

export function createProductsHref(filters, overrides = {}) {
  const values = { ...filters, ...overrides };
  const query = new URLSearchParams();

  if (values.search) query.set("search", values.search);
  if (values.brand) query.set("brand", values.brand);
  if (values.minPrice) query.set("minPrice", values.minPrice);
  if (values.maxPrice) query.set("maxPrice", values.maxPrice);

  if (values.sort && values.sort !== DEFAULT_SORT) {
    query.set("sort", values.sort);
  }

  if (values.page && values.page > 1) {
    query.set("page", String(values.page));
  }

  const queryString = query.toString();
  return queryString ? `/products?${queryString}` : "/products";
}
