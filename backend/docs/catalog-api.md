# Catalog API — دليل التيم

كل الشغل موجود داخل `backend` على .NET 10 وPostgreSQL. المسارات تبدأ بـ`/api/v1`، من غير كلمة `catalog` في الـURL.

## الصلاحيات

| المسار | مين يستخدمه؟ | البيانات |
|---|---|---|
| `/api/v1/...` | الزائر والعميل والموظف والأدمن | قراءة عامة؛ المنتجات والفاريانتس المتاحة فقط |
| `/api/v1/staff/...` | `staff` و`admin` | قراءة تشغيلية، تشمل المنتجات والفاريانتس غير المفعلة |
| `/api/v1/admin/...` | `admin` | إدارة الكتالوج بالكامل |
| `/api/v1/dashboard/...` | `staff` و`admin` | العدادات التشغيلية |

السياسات المركزية هي `CatalogRead` و`CatalogManage`؛ يمكن توسيعها عند إضافة أدوار جديدة. بعد الدمج، تسجيل الدخول وإصدار التوكن موجودان تحت `/api/auth`، وأدوار Identity هي `Customer/Staff/Admin`؛ سياسات الكتالوج تقبل أيضًا الصيغة الصغيرة السابقة. خطوات تسجيل الدخول موضحة في [التصنيفات](catalog-categories.md).

## التصنيفات

| Method | Endpoint |
|---|---|
| GET | `/categories` |
| GET | `/categories/{categoryId}` |
| GET, POST | `/admin/categories` |
| GET, PUT, DELETE | `/admin/categories/{categoryId}` |

القراءة العامة ترجع المعرف والاسمين. قراءة الأدمن تضيف عدد المنتجات. البحث والصفحات متاحان. حذف تصنيف فيه أي منتجات يرجع `409`؛ انقل المنتجات أولًا، حتى لو كانت غير مفعلة. [التفاصيل](catalog-categories.md).

## المنتجات

| Method | Endpoint |
|---|---|
| GET | `/products` |
| GET | `/products/{productId}` |
| GET | `/staff/products` |
| GET | `/staff/products/{productId}` |
| GET, POST | `/admin/products` |
| GET, PUT | `/admin/products/{productId}` |
| PATCH | `/admin/products/{productId}/status` |

القوائم تدعم البحث بالاسم أو الباركود، فلتر التصنيف، الصفحات، والترتيب. قوائم الموظف والأدمن تضيف فلتر الحالة. قراءة المنتج تشمل خياراته وقيمه وفاريانتس حسب الصلاحية. المنتج يظهر للعامة إذا كان مفعلًا وعنده فاريانت مفعل واحد على الأقل. تعطيل المنتج يحافظ على سطور الفواتير. [التفاصيل](catalog-products.md).

## الخيارات وقيمها

Base: `/admin/products/{productId}/options`.

| Method | Relative endpoint |
|---|---|
| GET, POST | `/` |
| GET, PUT, DELETE | `/{optionId}` |
| GET, POST | `/{optionId}/values` |
| GET, PUT, DELETE | `/{optionId}/values/{valueId}` |

نفس مسارات GET موجودة تحت `/staff/products/{productId}/options` و`/products/{productId}/options`. القراءة العامة تعرض الخيارات والقيم المستخدمة في فاريانتس متاحة. حذف خيار أو قيمة مستخدمة في أي فاريانت يرجع `409`، حتى لو الفاريانت غير مفعل. حذف الخيار غير المستخدم يحذف قيمه معه. [التفاصيل](catalog-options.md).

## الفاريانتس والباركود

مسارات القراءة التالية موجودة للعامة، وتحت `/staff`، وتحت `/admin`:

| Method | Endpoint |
|---|---|
| GET | `/variants` |
| GET | `/variants/{variantId}` |
| GET | `/variants/by-barcode/{barcode}` |
| GET | `/variants/by-barcode?barcode=...` |
| GET | `/products/{productId}/variants` |

استخدم صيغة query للباركود لو يحتوي رموزًا مثل `/`، مع URL encoding.

| Method | Admin endpoint |
|---|---|
| POST | `/admin/products/{productId}/variants` |
| PUT | `/admin/products/{productId}/variants/{variantId}` |
| PATCH | `/admin/products/{productId}/variants/{variantId}/status` |

الباركود فريد على مستوى الكتالوج، حتى للفاريانتس غير المفعلة. السعر بوحدة القرش، والوزن بالجرام. اختيار قيم الخيارات يتم في طلب إنشاء/تعديل الفاريانت، بقيمة واحدة كحد أقصى لكل خيار، وكل القيم لازم تكون من نفس المنتج. القائمة الفارغة مسموحة. الربط يتحدث داخل نفس عملية الحفظ. لا توجد كتابة مستقلة لجدول الربط. [التفاصيل](catalog-variants.md).

## الصور والداشبورد

| Method | Endpoint | الاستخدام |
|---|---|---|
| POST | `/admin/products/{productId}/image` | رفع/استبدال صورة، multipart field `file` |
| DELETE | `/admin/products/{productId}/image` | إزالة الصورة |
| GET | `/product-images/{fileName}` | قراءة صورة محلية عامة |
| GET | `/dashboard/products/summary` | ملخص المنتجات |
| GET | `/dashboard/summary` | ملخص التصنيفات والمنتجات والفاريانتس والخيارات |

[تفاصيل الصور](catalog-images.md) — [تفاصيل العدادات](catalog-dashboard.md).

## التشغيل والتحقق

من جذر المشروع:

```powershell
dotnet restore backend/SARE.sln
dotnet ef database update --project backend/SARE.Infrastructure --startup-project backend/SARE.Api
dotnet run --project backend/SARE.Api
dotnet test backend/SARE.sln
```

راجع [ملاحظات الدمج وتاريخ migrations](integration-startup.md) قبل تحديث قاعدة موجودة من فرع قديم. اضبط `ConnectionStrings:Database` باستخدام User Secrets أو `ConnectionStrings__Database` للاتصال بقاعدة PostgreSQL. إصدار التوكن والتحقق منه يستخدمان إعدادات `Jwt`. اضبط `ProductImages__StoragePath` على مجلد دائم قابل للكتابة؛ احفظه مع نسخة قاعدة البيانات الاحتياطية. كل feature لها commit مستقل على `codex/catalog-feature`، والدمج موجود على `codex/integration-startup`.

طلبات جاهزة في [SARE.Api.http](../SARE.Api/SARE.Api.http). استبدل IDs والتوكنات الموجودة في أول الملف بالنتائج الفعلية. الطلبات أمثلة مستقلة؛ لتجربة الرحلة كاملة، أنشئ التصنيف ثم المنتج ثم الخيار والقيمة ثم الفاريانت، واترك المنتج والفاريانت مفعلين أثناء تجربة القراءة العامة.

POST يرجع `201` و`Location` لإنشاء التصنيفات والمنتجات والخيارات والقيم والفاريانتس؛ رفع الصورة يرجع `200`. PUT/PATCH يرجعان `200`، وDELETE الناجح يرجع `204`. أخطاء البيانات `400`، المصادقة `401`، الصلاحيات `403`، العناصر غير الموجودة `404`، التعارضات `409`، والصورة الأكبر من الحد `413`. الأخطاء بصيغة Problem Details. الأسماء والقيم تتنظف من المسافات؛ حدود الأطوال موضحة في مستند كل feature.

تعديلات المنتج وتوابعه تحدث `updatedAt` تلقائيًا، والتعديل المتزامن أثناء معالجة الطلب يرجع `409` ويُلغي حفظ العملية بالكامل. عدّل البيانات بعد إعادة تحميلها عند التعارض. هذا الفحص يحمي الكتابات المتزامنة أثناء الطلب؛ لا يوجد حاليًا بروتوكول ETag/If-Match لمنع إرسال بيانات من شاشة قديمة.

اختبارات التكامل تستخدم SQLite مع توكنات JWT موقعة فعلًا، وتفحص الصلاحيات والحفظ والعلاقات والبحث والتعطيل والصور والعدادات. اختبار التعارض يستخدم تعديلًا فعليًا في قاعدة الاختبار؛ بعض استثناءات PostgreSQL تُحاكى. تشغيل migrations والاستعلامات على PostgreSQL فعلي يظل مطلوبًا في بيئة التيم.
