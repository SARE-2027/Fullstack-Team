# SARE — Smart Automated Retail Ecosystem

> **حالة المشروع:** أُنشئت الـ solution والمشاريع الأربعة داخل `backend/` باستخدام .NET 10. كُتبت كيانات وEnums طبقة Domain، وأُضيف `AppDbContext` وتهيئة الجداول والعلاقات في Infrastructure. الخدمات والـ API وباقي المكونات ما زالت ضمن التصميم المستهدف. لم تُنشأ migration أو قاعدة بيانات.

## نظرة عامة

يهدف **SARE** إلى تحويل عربة التسوق التقليدية إلى نقطة محاسبة ذاتية متحركة. يتسوق العميل ويتابع فاتورته من شاشة العربة، من غير الحاجة إلى تطبيق على هاتفه أو بوابات خروج معقدة في المتجر.

يعتمد النظام على دمج رؤية حاسوبية وقارئ باركود وحساسات وزن في العربة، مع إشارات اختيارية من رفوف ذكية مستقلة. يعالج الـ backend الأحداث، يتحقق من الصنف والوزن، يحفظ الجلسة والفاتورة، ويرسل التحديثات الحية إلى شاشة العربة وتابلت الموظفين.

## مكونات النظام

| المكون | الدور |
| --- | --- |
| Raspberry Pi 5 | تشغيل واجهة الكشك على شاشة 10.1 بوصة، نموذج الرؤية YOLO nano عبر NCNN، وCart Core بلغة Python. |
| ESP32 وأجهزة العربة | قراءة حساسات الوزن Load Cells مع HX711، قارئ NFC، قارئ باركود/QR ثنائي الأبعاد، والتحكم في WS2812B LED والجرس. |
| الرفوف الذكية | ESP32 مع حساس وزن لرصد سحب الصنف. يُقارن الحدث برصد العربة إذا ظهر حدث قريب للصنف نفسه. |
| Backend بـ .NET 10 | استقبال أحداث الأجهزة عبر MQTT، إدارة الكتالوج والجلسات والفواتير، اتخاذ قرارات المطابقة، وحفظ البيانات في PostgreSQL. |
| SignalR | إرسال تحديثات الفاتورة والحالة إلى شاشة العربة وتابلت الموظفين لحظيًا. |

## نطاق المستودع

```text
SARE/
├── PROJECT.md               # هذه الوثيقة
├── backend/                 # مشاريع .NET 10؛ تفاصيل البنية المستهدفة أدناه
│   └── SARE.sln
└── frontend/                # واجهات المستخدم؛ لم يُحدد هيكلها هنا
```

ملفات الـ solution والمشاريع الأربعة، وملفات Domain، و`Persistence/AppDbContext.cs` و`Persistence/Configurations/` موجودة داخل `backend/`. بقية المسارات التالية **تصميم مستهدف** لم يُنفذ بعد.

## هيكل الـ backend المستهدف

```text
backend/
├── SARE.sln
├── SARE.Domain/                         # الكيانات وقواعد العمل؛ بلا اعتماد على مكتبات خارجية
│   ├── Common/
│   │   └── BaseEntity.cs                # Guid Id فقط؛ حقول التاريخ ليست مشتركة بين كل الجداول
│   ├── Enums/
│   │   ├── CartStatus.cs               # Active, Disabled
│   │   ├── SessionStatus.cs            # Open, Closed, Abandoned
│   │   ├── UserRole.cs
│   │   ├── DetectionSource.cs
│   │   ├── DetectionOutcome.cs
│   │   └── CloseReason.cs              # StaffClosed, Abandoned
│   ├── Catalog/
│   │   ├── Category.cs
│   │   ├── Product.cs
│   │   ├── ProductOption.cs
│   │   ├── ProductOptionValue.cs
│   │   ├── ProductVariant.cs
│   │   └── VariantOptionValue.cs
│   ├── Users/
│   │   └── User.cs
│   └── Cart/
│       ├── Cart.cs
│       ├── Session.cs
│       ├── SessionItem.cs
│       ├── DetectionEvent.cs
│       └── ShelfEvent.cs
├── SARE.Application/                    # الخدمات، DTOs، التحقق ومحرك القرار
│   ├── Common/Interfaces/
│   │   ├── IProductRepository.cs
│   │   ├── ICartRepository.cs
│   │   ├── ISessionRepository.cs
│   │   ├── IMqttPublisher.cs
│   │   └── ICartNotifier.cs
│   ├── DTOs/
│   │   ├── Catalog/
│   │   │   ├── CreateProductRequest.cs
│   │   │   └── ProductBarcodeResponse.cs
│   │   ├── Cart/
│   │   │   ├── StartSessionRequest.cs
│   │   │   └── CartSummaryResponse.cs
│   │   └── Events/
│   │       ├── CartDetectionEventRequest.cs
│   │       └── ShelfEventRequest.cs
│   ├── Validators/
│   │   ├── Catalog/
│   │   │   └── CreateProductRequestValidator.cs
│   │   └── Cart/
│   │       ├── StartSessionRequestValidator.cs
│   │       └── CartDetectionEventRequestValidator.cs
│   ├── Services/
│   │   ├── CatalogService.cs
│   │   ├── SessionService.cs
│   │   └── DetectionEventService.cs
│   ├── DecisionEngine/
│   │   └── WeightToleranceChecker.cs
│   └── Extensions/
│       └── DependencyInjection.cs
├── SARE.Infrastructure/                 # تنفيذ قاعدة البيانات والاتصال بالأجهزة والتحديثات الحية
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── Catalog/
│   │   │   │   ├── ProductConfiguration.cs
│   │   │   │   ├── ProductOptionConfiguration.cs
│   │   │   │   ├── ProductOptionValueConfiguration.cs
│   │   │   │   ├── ProductVariantConfiguration.cs
│   │   │   │   └── VariantOptionValueConfiguration.cs
│   │   │   ├── Users/
│   │   │   │   └── UserConfiguration.cs
│   │   │   └── Cart/
│   │   │       ├── CartConfiguration.cs
│   │   │       ├── SessionConfiguration.cs
│   │   │       ├── SessionItemConfiguration.cs
│   │   │       ├── DetectionEventConfiguration.cs
│   │   │       └── ShelfEventConfiguration.cs
│   │   ├── Repositories/
│   │   │   ├── ProductRepository.cs
│   │   │   ├── CartRepository.cs
│   │   │   └── SessionRepository.cs
│   │   └── Migrations/
│   ├── IoT/
│   │   └── MqttPublisher.cs
│   ├── Realtime/
│   │   └── CartNotifier.cs
│   └── Extensions/
│       └── DependencyInjection.cs
└── SARE.Api/                            # نقطة تشغيل النظام ومنافذه
    ├── Controllers/
    │   ├── CatalogController.cs
    │   ├── SessionController.cs
    │   └── UsersController.cs
    ├── Hubs/
    │   └── CartHub.cs
    ├── BackgroundServices/
    │   ├── MqttCartEventsWorker.cs
    │   └── MqttShelfEventsWorker.cs
    ├── Middlewares/
    │   └── GlobalExceptionMiddleware.cs
    ├── Extensions/
    │   └── DependencyInjection.cs
    ├── appsettings.json
    ├── appsettings.Development.json
    └── Program.cs
```

### مسؤولية كل طبقة واتجاه الاعتماد

- **Domain:** الكيانات والقيم والحالات الأساسية، من دون EF Core أو MQTT أو SignalR. الكيانات تحتوي أعمدة الجداول وحقول الـ foreign key فقط، دون navigation collections.
- **Application:** حالات الاستخدام المباشرة، DTOs بصيغة records، التحقق بـ FluentValidation، وعقود الوصول إلى البيانات والأجهزة. يحتوي `WeightToleranceChecker` على قاعدة تفاوت الوزن المقترحة **±5 جرام**.
- **Infrastructure:** تنفيذ عقود Application باستخدام EF Core وPostgreSQL وMQTT وSignalR. أسماء الجداول والأعمدة تُستنتج تلقائيًا بصيغة `snake_case`؛ ملفات Configuration تحتفظ بالعلاقات والقيود غير المستنتجة تلقائيًا.
- **Api:** Controllers وSignalR Hub وعمال MQTT والخط الوسيط للأخطاء وإعدادات التشغيل.

اتجاه الاعتماد المقترح: `Application → Domain`، و`Infrastructure → Application + Domain`، و`Api → Application + Infrastructure`. تسجل طبقة Api التنفيذات في حقن الاعتماد.

## تدفق العمل الأساسي

1. تبدأ العربة جلسة تسوق مرتبطة بـ `cart_id`، مع `user_id` اختياري للزائر.
2. ترصد الكاميرا أو السكانر أو الإدخال اليدوي حدثًا، وترسل أجهزة العربة فرق الوزن ومعلومات الصنف عبر MQTT.
3. عند سحب منتج من رف ذكي، يصل `shelf_event` يحدد الرف والمنتج المعروف عليه وتغير وزنه.
4. إذا وصل رصد قريب من العربة بالكاميرا أو السكانر، تقارن خدمة الأحداث المنتج المرصود بمنتج الرف وقراءات الوزن. اتفاق الإشارتين يدعم اعتماد الرصد، واختلافهما يساعد في اكتشاف الخطأ أو تصحيحه. حدث الرف وحده لا يضيف صنفًا للفاتورة. تحدد الخدمة نتيجة الرصد: `accepted` أو `corrected` أو `rejected` أو `unknown`.
5. تحفظ أحداث الرصد والرفوف وسطور الفاتورة في PostgreSQL. سعر السطر هو السعر وقت الإضافة، وإزالة السطر تسجل في `removed_at`.
6. ترسل التغييرات إلى شاشة العربة وتابلت الموظفين عبر SignalR.

الجلسة نفسها هي الفاتورة في النطاق الحالي: تحتوي البنود والإجمالي والحالة.

## مخطط قاعدة البيانات المقترح

قاعدة البيانات PostgreSQL، وتتكون من **12 جدولًا**. `uuid` للمعرفات المنطقية، بينما معرف العربة والرف نصيان. تستخدم الأوقات `timestamptz`، وتحفظ الأسعار بوحدة القرش في حقول `*_minor` لتجنب الكسور العشرية. القيم التي لم يرد لها سماح صريح بـ `null` تعامل هنا كحقول مطلوبة في التصميم المبدئي.

تُستنتج أسماء الجداول من `DbSet` وأسماء الأعمدة من خصائص الكيانات، وتحوّلها تسمية `snake_case` تلقائيًا. تُخزّن قيم C# enums كنصوص صغيرة بصيغة `snake_case`، وتتحقق `CHECK` constraints من القيم المسموحة. تُعرّف العلاقات عبر أعمدة الـ foreign key في ملفات Configuration، دون navigation properties في الكيانات.

### 1. الكتالوج والمنتجات

| الجدول | الأعمدة والقيود |
| --- | --- |
| `categories` | `id uuid PK`؛ `name_ar text`؛ `name_en text`. |
| `products` | `id uuid PK`؛ `category_id uuid FK → categories.id`؛ `name_ar text`؛ `name_en text`؛ `image_url text NULL`؛ `is_active bool`؛ `updated_at timestamptz` لمزامنة الـ Pi. |
| `product_options` | `id uuid PK`؛ `product_id uuid FK → products.id ON DELETE CASCADE`؛ `name_ar text`؛ `name_en text`؛ مثل الحجم أو النكهة. |
| `product_option_values` | `id uuid PK`؛ `product_option_id uuid FK → product_options.id ON DELETE CASCADE`؛ `value_ar text`؛ `value_en text`؛ مثل عائلي أو جبنة. |
| `product_variants` | `id uuid PK`؛ `product_id uuid FK → products.id`؛ `barcode varchar(64) UNIQUE NOT NULL`؛ `price_minor int`؛ `weight_g int`؛ `is_active bool`؛ `updated_at timestamptz`. الفاريانت هو السلعة المادية ذات الباركود والسعر والوزن. |
| `variant_option_values` | `variant_id uuid FK → product_variants.id ON DELETE CASCADE`؛ `option_value_id uuid FK → product_option_values.id`؛ مفتاح مركب `PK (variant_id, option_value_id)`. |

### 2. المستخدمون والعربات

| الجدول | الأعمدة والقيود |
| --- | --- |
| `carts` | `id text PK` مثل `C12`؛ `token_hash text`؛ `status text` (`active`, `disabled`)؛ `battery_pct smallint NULL`؛ `last_seen_at timestamptz NULL`؛ `sw_version text NULL`. هوية العربة مستقلة عن حسابات المستخدمين وأدوارهم. |
| `users` | `id uuid PK`؛ `name text`؛ `email text NULL UNIQUE`؛ `password_hash text NULL`؛ `role text`؛ `nfc_uid text NULL UNIQUE`؛ `is_active bool`؛ `created_at timestamptz`. الأدوار: `customer`, `staff`, `admin`. |

### 3. الجلسات والفواتير والأحداث

| الجدول | الأعمدة والقيود |
| --- | --- |
| `sessions` | `id uuid PK`؛ `cart_id text FK → carts.id`؛ `user_id uuid NULL FK → users.id`؛ `status text`؛ `total_minor int`؛ `started_at timestamptz`؛ `last_activity_at timestamptz`؛ `closed_at timestamptz NULL`؛ `closed_by uuid NULL FK → users.id`؛ `close_reason text NULL`. الجلسة هي الفاتورة، وحالاتها هنا: `open`, `closed`, `abandoned`. أسباب الإغلاق: `staff_closed`, `abandoned`. |
| `session_items` | `id uuid PK` يولده الـ Pi لمنع التكرار؛ `session_id uuid FK → sessions.id`؛ `variant_id uuid FK → product_variants.id`؛ `unit_price_minor int`؛ `source text`؛ `added_at timestamptz`؛ `removed_at timestamptz NULL`. |
| `detection_events` | `id uuid PK` يولده الـ Pi؛ `session_id uuid FK → sessions.id`؛ `session_item_id uuid NULL FK → session_items.id`؛ `source text`؛ `detected_barcode varchar(64) NULL`؛ `confidence real NULL`؛ `weight_delta_g int`؛ `outcome text`؛ `final_barcode varchar(64) NULL`؛ `model_version varchar(50) NULL`؛ `created_at timestamptz` وقت حدوث الرصد على الـ Pi. |
| `shelf_events` | `id uuid PK`؛ `shelf_id text`؛ `variant_id uuid FK → product_variants.id`؛ `weight_delta_g int`؛ `is_matched bool`؛ `matched_session_id uuid NULL FK → sessions.id`؛ `created_at timestamptz`. السحب من الرف يمثل بقيمة وزن سالبة. |

### القيم المشتركة

| النوع | القيم المقترحة |
| --- | --- |
| `DetectionSource` | `vision`, `scanner`, `manual` |
| `DetectionOutcome` | `accepted`, `corrected`, `rejected`, `unknown` |
| `SessionStatus` | `open`, `closed`, `abandoned` |
| `UserRole` | `customer`, `staff`, `admin` |
| `CloseReason` | `staff_closed`, `abandoned` |
| `CartStatus` | `active`, `disabled` |

## قواعد مهمة عند التنفيذ لاحقًا

- يجب أن يكون `product_variants.barcode` فريدًا، ومفتاح `variant_option_values` مركبًا.
- تمنع قيود قاعدة البيانات السعر أو الإجمالي السالب، وتتحقق من أن وزن الصنف موجب، ونسبة البطارية بين 0 و100، وثقة الرؤية بين 0 و1 عند وجودها.
- تتحقق القيود من القيم النصية المسموحة للـ enums، ومن أن وقت إزالة الصنف لا يسبق إضافته ووقت إغلاق الجلسة لا يسبق بدايتها.
- تحفظ الأسعار في الفاتورة وقت إضافة المنتج، حتى لو تغير سعر الكتالوج بعد ذلك.
- يحدّث `AppDbContext` قيمة `products.updated_at` و`product_variants.updated_at` تلقائيًا بتوقيت UTC عند إضافة أو تعديل الكيان عبر `SaveChanges` أو `SaveChangesAsync`. التحديثات المباشرة عبر `ExecuteUpdate` أو SQL لا تمر بهذا المسار، وعليها تعيين الوقت صراحةً إذا استُخدمت لاحقًا.
- حذف صنف من الجلسة يسجل `removed_at` بدل حذف السجل، لتبقى حركة الجلسة قابلة للتتبع.
- معرفات الأحداث وسطور الفاتورة القادمة من الـ Pi تستخدم لمنع معالجة الرسالة نفسها مرتين عند إعادة إرسالها؛ يلزم تحديد سلوك إعادة المحاولة ومعاملات قاعدة البيانات أثناء التنفيذ.
- الفهرس المركب على `detection_events(session_id, created_at)` يخدم عرض أحداث جلسة بالترتيب. ينشئ EF فهرس `session_item_id` للـ foreign key تلقائيًا.
- `detection_events.created_at` يأتي من الحدث الأصلي على الـ Pi؛ لا يُعطى قيمة `now()` افتراضية حتى لا يُستبدل وقت الرصد بوقت وصول الرسالة إلى السيرفر.
- يقارن فحص الوزن بين التغير الفعلي والوزن النظري للصنف ضمن تفاوت مقترح **±5g**؛ يلزم تحديد كيفية التعامل مع أكثر من صنف أو القراءات المتزامنة.
- إعدادات PostgreSQL وMQTT وJWT ستكون في إعدادات الـ Api، مع حفظ الأسرار خارج الملفات الملتزم بها في المستودع عند بدء التنفيذ.

## قرارات النطاق الحالي

- حالة العربة في البيانات `active` أو `disabled`، وتستخدم العربة `token_hash` لهويتها. أدوار `customer` و`staff` و`admin` تخص المستخدمين، وليست حالات للعربة.
- الجلسة هي الفاتورة: تضم الأصناف والإجمالي وحالة الجلسة.
- يظل MQTT وسيلة نقل أحداث العربة والرف كما في الهيكل المقترح، من غير تثبيت topics أو صيغة تفصيلية للرسائل الآن.
- الرف الذكي يرسل حدثًا عن المنتج المعروف على الرف وتغير الوزن. عند وصول رصد قريب من العربة، تُقارن الإشارتان للتحقق من المنتج أو تصحيح الرصد عند الاختلاف. لا يُنشئ حدث الرف وحده بند فاتورة.
