# Product options and values

Admin base: `/api/v1/admin/products/{productId}/options`.

| Method | Relative route | Operation |
| --- | --- | --- |
| GET | `/` | List options with their values. |
| GET | `/{optionId}` | Read an option. |
| POST | `/` | Create with `{ "nameAr": "الحجم", "nameEn": "Size" }`. |
| POST | `/with-values` | Create an option and one or more values in one atomic request. |
| PUT | `/{optionId}` | Replace both names. |
| DELETE | `/{optionId}` | Delete an unused option and its values. |
| GET | `/{optionId}/values` | List values. |
| GET | `/{optionId}/values/{valueId}` | Read a value. |
| POST | `/{optionId}/values` | Create with `{ "valueAr": "صغير", "valueEn": "Small" }`. |
| PUT | `/{optionId}/values/{valueId}` | Replace both values. |
| DELETE | `/{optionId}/values/{valueId}` | Delete an unused value. |

The four GET routes also exist under `/api/v1/staff/products/{productId}/options` for staff/admin, and `/api/v1/products/{productId}/options` for everyone. Public reads expose options/values used by active variants of an available product. Staff reads include all options/values. All writes require the `CatalogManage` policy (admin).

Product lists/details already include options and their values. Variant reads include the same permitted choices with `selectedValueId` per option, so opening a variant directly does not require a separate options request. These focused GET routes remain available for refreshing option editors. Variant associations are updated through variant POST/PUT with `optionValueIds`.

Names/values are trimmed, required in both languages, and limited to 100 characters. POST returns `201` with a `Location` header; PUT returns `200`; DELETE returns `204`. Incorrect parent/child relationships return `404`. Deleting any option/value referenced by a variant, including an inactive one, returns `409`. Changes also update the parent product's UTC timestamp. Database changes are saved atomically.

## Create an option with values

`POST /api/v1/admin/products/{productId}/options/with-values` requires an admin token:

```json
{
  "nameAr": "اللون",
  "nameEn": "Color",
  "values": [
    { "valueAr": "أحمر", "valueEn": "Red" },
    { "valueAr": "أزرق", "valueEn": "Blue" }
  ]
}
```

The response is the existing `ProductOptionResponse`: the new option ID, both names,
and `values` containing each generated ID and both labels. `Location` points to the
existing option GET route. The values list must contain at least one valid value;
missing, empty, or null values are rejected with `400` before anything is saved.
The option, all values, and the product timestamp are committed in a single save;
a concurrent parent edit returns `409` and rolls back the new option and values.

The frontend can immediately put a returned value ID into the variant's
`optionValueIds` and save the variant through its existing POST/PUT endpoint.
Creating an option does not automatically select it on any variant. The original
option-only and value-only endpoints remain available. No catalog SignalR events
are published by this operation.
