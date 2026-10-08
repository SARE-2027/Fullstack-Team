# Dashboard summary

`GET /api/v1/dashboard/summary` requires a signed JWT with `staff` or `admin` role. Anonymous requests return `401`; customer requests return `403`. The existing `/api/v1/dashboard/products/summary` remains available for a product-only dashboard.

Example response:

```json
{
  "totalCategories": 2,
  "totalProducts": 3,
  "activeProducts": 2,
  "inactiveProducts": 1,
  "availableProducts": 1,
  "productsWithoutActiveVariants": 1,
  "totalVariants": 3,
  "activeVariants": 2,
  "inactiveVariants": 1,
  "availableVariants": 1,
  "totalOptions": 1,
  "totalOptionValues": 2
}
```

Active/inactive counters refer to each row's own status flag. Available variants require both the variant and its product to be active. Available products require an active product and at least one active variant. `productsWithoutActiveVariants` includes all products, regardless of their status. Categories include empty categories, and option/value counts include unused options/values and inactive parents.

All counters are computed in one SQL statement to share a database snapshot. An empty catalog returns all zeros. This dashboard covers catalog data; sessions, invoices, carts, and users have separate future features.
