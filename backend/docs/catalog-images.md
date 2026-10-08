# Product images

| Method | Route | Access | Result |
|---|---|---|---|
| POST | `/api/v1/admin/products/{productId}/image` | Admin | Upload/replace image; `200` product summary |
| DELETE | `/api/v1/admin/products/{productId}/image` | Admin | Clear image; `204` (also if already empty) |
| GET | `/api/v1/product-images/{fileName}` | Public | Image bytes, or `404` |

POST uses `multipart/form-data` with a required `file` field. Maximum file size: **5 MiB**. PNG, JPEG, and WebP signatures are accepted; filenames and client MIME types do not determine storage names or response types. Invalid/empty files return `400`, oversized files `413`, missing products `404`, concurrent product edits `409`. The image endpoint checks file signatures, not full image decoding.

Generated URLs are returned in `imageUrl`. Public image responses use a fixed image MIME type and `X-Content-Type-Options: nosniff`. Original filenames are discarded. Uploads use unique names and are stored outside the web root. Image bytes can be accessed publicly even when the parent product is inactive; inactive product details stay hidden on public product routes.

Configure `ProductImages:StoragePath` to a persistent writable directory (environment variable `ProductImages__StoragePath`). The default is `App_Data/product-images` under the application output directory. Keep that directory when redeploying; multiple API instances need shared storage. Include it in backups along with the database.

New files are written before saving their URL. If saving fails, the new file is removed. Replacement/deletion removes the previous managed file only after a successful database save; external URLs are never deleted. Cleanup failures are logged. A process crash between disk and database operations can leave orphan files; disk and PostgreSQL are separate stores.

Managed relative URLs are assigned through upload only. Product metadata updates may preserve their existing managed URL, but cannot assign another managed image. Changing a managed URL to an external URL or null also cleans up its old file after saving. External HTTP(S) image URLs remain supported in normal product requests.

Tests cover upload/read/replace/delete, timestamps, validation, stream size, unsafe paths, authorization, and file cleanup after database concurrency conflicts.
