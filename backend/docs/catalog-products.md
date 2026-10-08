# Products API

This feature adds product management, customer browsing, staff inspection, and product dashboard statistics on the `codex/catalog-feature` branch. The current domain roles are `customer`, `staff`, and `admin`.

## Permissions and endpoints

| Method | Route | Access | Behavior |
| --- | --- | --- | --- |
| GET | `/api/v1/products` | Everyone, including guests | Search, category filter, sorting, and pagination over available products. |
| GET | `/api/v1/products/{productId}` | Everyone, including guests | Product details, active variants, and the option values used by those variants. |
| GET | `/api/v1/staff/products` | Staff and admin | All products, including inactive products; search, filters, sorting, and pagination. |
| GET | `/api/v1/staff/products/{productId}` | Staff and admin | Full product details, including inactive variants and all options/values. |
| GET | `/api/v1/admin/products` | Admin | Full product listing with filters and variant counts. |
| GET | `/api/v1/admin/products/{productId}` | Admin | Full product details. |
| POST | `/api/v1/admin/products` | Admin | Create a product, returning `201` and its detail URL in `Location`. |
| PUT | `/api/v1/admin/products/{productId}` | Admin | Replace the product's basic fields. |
| PATCH | `/api/v1/admin/products/{productId}/status` | Admin | Activate or deactivate a product. |
| GET | `/api/v1/dashboard/products/summary` | Staff and admin | Product statistics for dashboards. |

Policies are defined in `AuthorizationPolicies`: `CatalogRead` permits staff/admin operational reads; `CatalogManage` permits admin management. New roles must be explicitly granted access in the policies. Customers can use the public product routes. Unauthenticated access to protected routes returns `401`; authenticated users without the required permission receive `403`.

## Create and update

POST and PUT accept:

```json
{
  "categoryId": "82cd1b5f-ea3b-4d94-91a2-83a11e735ab1",
  "nameAr": "شيبسي",
  "nameEn": "Chips",
  "imageUrl": "https://example.com/chips.jpg",
  "isActive": true
}
```

- `categoryId` must identify an existing category.
- Both names are required, trimmed, and limited to 150 characters after trimming.
- `imageUrl` is optional, trimmed, and limited to 500 characters. Empty text becomes `null`. Accepted locations are absolute HTTP(S) URLs or local paths beginning with a single `/`; other schemes and network paths are rejected.
- `isActive` defaults to `true` if omitted. PUT replaces the basic fields; supply the desired status explicitly when editing.

The response includes basic fields, UTC `updatedAt`, both category names, `variantCount`, and `activeVariantCount`. `AppDbContext` stamps actual changes automatically. A no-op edit keeps the existing timestamp. The product price/weight remain on variants; they are not copied into the product table.

PUT can move the product to another category. Its ID, options, variants, and invoice history remain associated with the same product. Creating a product does not create variants.

The status endpoint accepts:

```json
{
  "isActive": false
}
```

The `isActive` field is required on PATCH. Deactivation changes the parent product only. Each variant retains its individual status, and reactivation makes eligible variants visible again. Product deletion is not exposed; deactivation preserves references and invoice history.

## Lists and search

| Query parameter | Default | Rules |
| --- | --- | --- |
| `search` | none | Trimmed substring search over Arabic/English product names and variant barcodes. Maximum 150 characters. Search is case insensitive; wildcard characters are literal text. |
| `categoryId` | none | Optional non-empty GUID. A category with no matching products returns an empty page. |
| `page` | `1` | Positive integer with a supported integer offset. |
| `pageSize` | `20` | From 1 to 100. |
| `sortBy` | `nameEn` | `nameAr`, `nameEn`, or `updatedAt`; names are case insensitive. |
| `sortDescending` | `false` | Controls sort direction; ID is the stable tie breaker. |
| `isActive` | none | Staff/admin lists only: optional product status filter. |

Lists return `items`, `totalCount`, `page`, and `pageSize`. `totalCount` counts matching products before pagination. Search/filtering/projection/counts run in the database; variants are not loaded just to count or search them.

## Customer visibility and details

A product is available when its own `isActive` flag is true and it has at least one active variant. A newly created product without variants remains visible to staff/admin while it is prepared; the public API returns it once an active variant exists. Inactive and unavailable product detail URLs return `404`.

Public listings include category names and the minimum/maximum prices among active variants, in minor currency units. Public barcode searches consider active variants only. Staff/admin barcode searches can also find inactive variants.

Both detail endpoints return a `product` object, `options`, and `variants`. Public detail includes only active variants and the option values those variants use. Staff/admin detail includes all variants, options, and values. Admin detail carries variant status and update timestamps; public detail provides the available variant's barcode, price, weight, and selected option value IDs.

## Dashboard

```json
{
  "totalProducts": 5,
  "activeProducts": 4,
  "inactiveProducts": 1,
  "productsWithoutActiveVariants": 2,
  "availableProducts": 2
}
```

`activeProducts` counts the parent status flag. `productsWithoutActiveVariants` counts all products without an active variant, regardless of parent status. `availableProducts` uses the public availability rule. Counts are read in one database statement. An empty catalog returns zero for every count.

## Errors and verification

- `400`: Invalid body/query or a nonexistent category selected in a product request.
- `401` / `403`: Authentication or permission failure on protected routes.
- `404`: Missing product, or a product unavailable through a public detail URL.
- `409`: The selected category was removed while the product operation was being saved.

Errors use Problem Details. The existing foreign key enforces category references, including concurrent changes. No new migration is needed.

Run `dotnet test backend/SARE.sln`. Integration tests use isolated SQLite databases and signed JWTs. They cover the role boundaries, persistence, search/filtering/sorting, public availability, nested option/variant visibility, timestamps, category validation, dashboards, and invoice preservation after deactivation. PostgreSQL concurrency exceptions are simulated; these tests do not connect to a live PostgreSQL database.

See `SARE.Api/SARE.Api.http` for requests and [category documentation](catalog-categories.md) for development token setup. Image upload, option/value management, and variant management are subsequent endpoint groups; this feature reads existing related records and accepts an image URL.
