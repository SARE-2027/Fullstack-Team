# Products API

This feature adds product management, customer browsing, staff inspection, and product dashboard statistics. The integrated Identity roles are `Customer`, `Staff`, and `Admin`. See [integration notes](integration-startup.md).

## Permissions and endpoints

| Method | Route | Access | Behavior |
| --- | --- | --- | --- |
| GET | `/api/v1/products` | Everyone, including guests | Search, category filter, sorting, and pagination over available products. |
| GET | `/api/v1/products/{productId}` | Everyone, including guests | Product details and options/values available through active variants. |
| GET | `/api/v1/staff/products` | Staff and admin | All products, including inactive products; search, filters, sorting, and pagination. |
| GET | `/api/v1/staff/products/{productId}` | Staff and admin | Product details and all options/values. |
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
  "isActive": true,
  "weightTolerancePercent": 3
}
```

- `categoryId` must identify an existing category.
- Both names are required, trimmed, and limited to 150 characters after trimming.
- `imageUrl` is optional, trimmed, and limited to 500 characters. Empty text becomes `null`. Accepted locations are absolute HTTP(S) URLs or local paths beginning with a single `/`; other schemes and network paths are rejected.
- `isActive` defaults to `true` if omitted. PUT replaces the basic fields; supply the desired status explicitly when editing.
- `weightTolerancePercent` is optional: `null` uses `CartMovement:DefaultWeightTolerancePercent`
  (3% by default). An explicit value from 0 to 100, with at most two decimal places,
  overrides that setting for every Variant of this Product. Zero means no package-weight
  allowance. PUT replaces this field too; send the current value to retain an override.

The response includes basic fields, UTC `updatedAt`, both category names, `variantCount`, and `activeVariantCount`. `AppDbContext` stamps actual changes automatically. A no-op edit keeps the existing timestamp. The product price/weight remain on variants; they are not copied into the product table.

Admin product responses, including lists/details and write responses, also include
`weightTolerancePercent` (the nullable saved override) and
`effectiveWeightTolerancePercent` (the override or configured default). The percentage
applies to each Variant's own gross packaged `weightG`; for example, 3% allows ±3g
on a 100g Variant and ±15g on a 500g Variant. Internal checks round fractional gram
allowances upward. Invoice/audit pieces save the effective percentage with their
weight; later Product edits do not change existing purchase snapshots. See
[cart weight and recount rules](cart-staff-audit.md#weight-tolerance).

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

Each product in a list includes `options`, with each option's `values`, as in product details. Options/values are bulk-loaded only for products on the requested page. Public reads include choices used by active variants; staff/admin reads include all options/values. Lists do not return a `variants` array. Product write responses retain their basic fields and omit `options`. `ProductSummaryResponse` is the public product listing contract; dashboard counts use the separate `ProductDashboardResponse`.

## Customer visibility and details

A product is available when its own `isActive` flag is true and it has at least one active variant. A newly created product without variants remains visible to staff/admin while it is prepared; the public API returns it once an active variant exists. Inactive and unavailable product detail URLs return `404`.

Public listings include category names and the minimum/maximum prices among active variants, in minor currency units. Public barcode searches consider active variants only. Staff/admin barcode searches can also find inactive variants.

Product detail endpoints return a `product` object and `options`, each with its `values`. They do not return variants. Public detail includes options/values used by active variants; staff/admin detail includes all options/values. The existing `/products/{productId}/variants` routes return paginated variants separately. Existing product counts, price ranges, and dashboard endpoints are unchanged.

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
- `409`: The selected category was removed while saving, or another request changed the product while this request was being processed.

Errors use Problem Details. The existing foreign key enforces category references, including concurrent changes. Product timestamps are optimistic concurrency tokens; apply the `ProtectCatalogEdits` mapping migration along with the initial schema.

Run `dotnet test backend/SARE.sln`. Integration tests use isolated SQLite databases and signed JWTs. They cover the role boundaries, persistence, search/filtering/sorting, public availability, nested option/variant visibility, timestamps, category validation, dashboards, and invoice preservation after deactivation. PostgreSQL concurrency exceptions are simulated; these tests do not connect to a live PostgreSQL database.

See `SARE.Api/SARE.Api.http` for requests and [category documentation](catalog-categories.md) for development token setup. [Option/value management](catalog-options.md), [variant management and barcode lookup](catalog-variants.md), [image uploads](catalog-images.md), and the [complete dashboard summary](catalog-dashboard.md) are implemented. The [catalog API index](catalog-api.md) lists the routes and access rules.
