# Variants and barcode lookup

Read bases are `/api/v1` (everyone, available variants only), `/api/v1/staff` (staff/admin, all variants), and `/api/v1/admin` (admin).

| Method | Relative route | Behavior |
| --- | --- | --- |
| GET | `/variants` | Search and paginated listing. |
| GET | `/variants/{variantId}` | Read variant details. |
| GET | `/variants/by-barcode/{barcode}` | Exact barcode lookup. |
| GET | `/variants/by-barcode?barcode=...` | Exact lookup supporting arbitrary characters, including encoded slashes. |
| GET | `/products/{productId}/variants` | Paginated variants for one product. |
| POST | `/products/{productId}/variants` | Admin: create a variant and its option associations. |
| PUT | `/products/{productId}/variants/{variantId}` | Admin: replace fields and option associations atomically. |
| PATCH | `/products/{productId}/variants/{variantId}/status` | Admin: activate/deactivate; body `{ "isActive": false }`. |

POST/PUT body:

```json
{
  "barcode": "6221234567890",
  "priceMinor": 1500,
  "weightG": 100,
  "isActive": true,
  "optionValueIds": []
}
```

The barcode is trimmed, required, at most 64 characters, and globally unique, including inactive variants. Price is in minor currency units and must be nonnegative; weight is a positive number of grams. `optionValueIds` is required; an empty list is valid. Selected IDs must exist under this product, contain no duplicates/empty IDs, and choose at most one value per option. Updates keep unchanged associations and add/remove the difference. The association table has no standalone write endpoints.

Query parameters: `search` (name/barcode, case insensitive, maximum 150 characters), `productId`, `categoryId`, `page` (default 1), `pageSize` (default 20, maximum 100). Staff/admin lists also accept `isActive` for the variant's own status. Lists are ordered by barcode then ID. Nested routes always enforce their product ID over a query-supplied ID.

Public variants require both product and variant to be active; unavailable IDs/barcodes return `404`. The response includes product/category IDs and product names for scanner/cart use. Disabling a variant preserves its links and invoice references; it continues reserving its barcode. A product with no active variants is unavailable through public product routes.

POST returns `201` and Location; PUT/PATCH return `200`. Invalid inputs/mappings return `400`; barcode conflicts and concurrent changes return `409`; wrong parent/child IDs return `404`.

All catalog child writes update the parent product timestamp. That timestamp is also an optimistic concurrency token: if another request modifies the product after it was read, EF rejects the stale write and rolls back the entire operation. `ProtectCatalogEdits` captures this mapping; it adds no columns or tables. Apply migrations normally with `dotnet ef database update --project backend/SARE.Infrastructure --startup-project backend/SARE.Api`.
