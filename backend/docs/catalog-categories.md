# Catalog categories API

Admin base route: `/api/v1/admin/categories`.

The five admin endpoints require a valid JWT bearer token with the `admin` role (lowercase, matching the domain role). Login and token issuance are a separate feature; these endpoints do not issue tokens.

Users and guests can read categories through `/api/v1/categories`, without an admin role or a token. These routes support GET only and return the category ID and both names. All categories are readable, including empty categories, since the category model has no visibility/status field. Administrative product counts are returned only by the admin routes.

## Admin endpoints

| Method | Route | Success | Behavior |
| --- | --- | --- | --- |
| GET | `/` | `200` | Search, pagination, and product count per category. |
| GET | `/{categoryId}` | `200` | Category details with product count. |
| POST | `/` | `201` | Create a category; response includes a `Location` header. |
| PUT | `/{categoryId}` | `200` | Replace both category names. |
| DELETE | `/{categoryId}` | `204` | Delete only when there are no products in the category. |

## User endpoints

| Method | Full route | Success | Behavior |
| --- | --- | --- | --- |
| GET | `/api/v1/categories` | `200` | Search and pagination; category ID and both names. |
| GET | `/api/v1/categories/{categoryId}` | `200` | Category ID and both names. |

User requests use the same search and pagination parameters as admin requests. POST, PUT, and DELETE on user routes return `405`; management operations use the protected admin routes.

`categoryId` is a GUID. An absent category returns `404`; an invalid GUID in the route does not match the endpoint and also returns `404`.

## Requests and responses

POST and PUT accept the same body:

```json
{
  "nameAr": "مشروبات",
  "nameEn": "Drinks"
}
```

Both names are required, trimmed before validation/storage, and limited to 100 characters after trimming. Duplicate names are allowed by the current schema; no uniqueness rule has been added.

An individual response:

```json
{
  "id": "82cd1b5f-ea3b-4d94-91a2-83a11e735ab1",
  "nameAr": "مشروبات",
  "nameEn": "Drinks",
  "productCount": 3
}
```

List query parameters:

| Parameter | Default | Rules |
| --- | --- | --- |
| `search` | none | Trimmed substring search over both names, ignoring letter case. Maximum 100 characters. `%` and `_` are treated as literal search text. |
| `page` | `1` | Positive integer; offsets outside the supported integer range are rejected. |
| `pageSize` | `20` | Integer from 1 to 100. |

Example: `GET /api/v1/admin/categories?search=drinks&page=1&pageSize=20`.

```json
{
  "items": [],
  "totalCount": 0,
  "page": 1,
  "pageSize": 20
}
```

Results are ordered by English name and then ID for stable pagination. `totalCount` counts categories matching the search before pagination. `productCount` includes active and inactive products and is computed in the database without loading the products into memory. An empty result or a page after the last page returns `200` with an empty `items` array.

## Errors and deletion

Errors use Problem Details (`application/problem+json`); validation errors include an `errors` dictionary with camelCase field names.

- `400`: Invalid names, query parameters, or JSON body.
- `401`: Missing, invalid, or expired token.
- `403`: Authenticated user without the `admin` role.
- `404`: Category does not exist.
- `409`: Category contains any products, including inactive products. Move them to a different category first.

The application checks for products before deletion. PostgreSQL's existing restricted foreign key also prevents a concurrent product insertion from allowing an invalid deletion; that specific constraint violation is translated to `409`. Concurrent deletion of the same category is translated to `404`. No schema changes or new migrations are required for this feature.

## Local development

Apply the existing migration to your configured PostgreSQL database if necessary, then run the API:

```powershell
dotnet ef database update --project backend/SARE.Infrastructure --startup-project backend/SARE.Api
dotnet run --project backend/SARE.Api --launch-profile http
```

Generate a development admin token in another terminal:

```powershell
dotnet user-jwts create --project backend/SARE.Api --role admin
```

Send the returned token as `Authorization: Bearer <token>`. Development signing keys are stored in user secrets. The command also sets local issuer/audience configuration. Production must configure a trusted token issuer and verification keys; the development token command is for local use. See [Microsoft's development JWT documentation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn?view=aspnetcore-10.0).

Request examples are available in `SARE.Api/SARE.Api.http`.

## Verification

```powershell
dotnet test backend/SARE.sln
```

The API integration tests exercise real HTTP routing, validation, the application service, repository queries, persistence, and signed JWT authentication. Each test uses a separate SQLite database. The PostgreSQL foreign-key race is simulated by injecting the provider exception; running these tests does not require or modify a PostgreSQL database.
