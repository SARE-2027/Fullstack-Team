# Product options and values

Admin base: `/api/v1/admin/products/{productId}/options`.

| Method | Relative route | Operation |
| --- | --- | --- |
| GET | `/` | List options with their values. |
| GET | `/{optionId}` | Read an option. |
| POST | `/` | Create with `{ "nameAr": "الحجم", "nameEn": "Size" }`. |
| PUT | `/{optionId}` | Replace both names. |
| DELETE | `/{optionId}` | Delete an unused option and its values. |
| GET | `/{optionId}/values` | List values. |
| GET | `/{optionId}/values/{valueId}` | Read a value. |
| POST | `/{optionId}/values` | Create with `{ "valueAr": "صغير", "valueEn": "Small" }`. |
| PUT | `/{optionId}/values/{valueId}` | Replace both values. |
| DELETE | `/{optionId}/values/{valueId}` | Delete an unused value. |

The four GET routes also exist under `/api/v1/staff/products/{productId}/options` for staff/admin, and `/api/v1/products/{productId}/options` for everyone. Public reads expose options/values used by active variants of an available product. Staff reads include all options/values. All writes require the `CatalogManage` policy (admin).

Names/values are trimmed, required in both languages, and limited to 100 characters. POST returns `201` with a `Location` header; PUT returns `200`; DELETE returns `204`. Incorrect parent/child relationships return `404`. Deleting any option/value referenced by a variant, including an inactive one, returns `409`. Changes also update the parent product's UTC timestamp. Database changes are saved atomically.
