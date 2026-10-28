# تسجيل وتجهيز كارت جديدة

## التسجيل الأول من جهاز الـhardware

البرنامج واحد لكل الأجهزة. هوية كل كارت محفوظة في ملف إعدادات دائم على الـSD card، وليست قيمة ثابتة داخل الكود أو كاش في الذاكرة.

1. قبل الاتصال، يقرأ الجهاز الـ`cartId` والـ`hardwareToken` من ملفه المحلي.
2. لو لم يُجهّز الجهاز بعد ولا توجد بيانات هوية محفوظة، يتصل بـSignalR على `/hubs/cart` وينادي `RegisterCart` بطلب `{}` أو `{ "swVersion": "pi-v1" }`.
3. السيرفر يسجّل كارت جديدة ويرجع `CartRegistrationResponse` الموضحة بالأسفل. الجهاز يحفظ الـID والتوكن الأصلي معًا في ملف دائم قبل المتابعة؛ لا يحفظ hash بدل التوكن.
4. بعد نجاح الحفظ، ينادي `JoinCartGroup(cartId, hardwareToken)` باستخدام البيانات المستلمة.
5. في كل تشغيل أو reconnect لاحق، يستخدم نفس البيانات وينادي `JoinCartGroup` فقط.

`RegisterCart` لا تحتاج JWT أو كود تجهيز، لكنها تعمل فقط والتسجيل مفتوح. التسجيل **مقفول افتراضيًا**، والأدمن يفتحه من الـHub بـ`SetCartRegistrationOpen(true)` وقت تجهيز العربات ثم يقفله بـ`SetCartRegistrationOpen(false)`، ويقرأ حالته بـ`GetCartRegistrationOpen()`. وهو مقفول ترجع `RegisterCart` خطأ `REGISTRATION_CLOSED` ولا يُحفظ شيء.
الحالة flag في ذاكرة السيرفر فقط (مشترك بين كل الاتصالات)، فأي restart يرجّعه مقفول. القفل يمنع التسجيل الجديد فقط؛ العربات المسجّلة تكمل `JoinCartGroup` عادي.
التسجيل لا ينضم للجروبات، ولا يبدأ جلسة، ولا يبث توكن الجهاز لأي مشترك؛ بيانات التجهيز ترجع لصاحب الاستدعاء فقط.

مثال توضيحي للبرنامج على الجهاز، مع دوال قراءة وحفظ ينفذها فريق الـhardware:

```javascript
connection.on("SessionUpdated", session => {
  // Display the session and its items; use status and version.
});
await connection.start();

let credentials = await loadDeviceCredentials();
if (credentials === null) {
  const registration = await connection.invoke("RegisterCart", { swVersion: "pi-v1" });
  credentials = {
    cartId: registration.cartId,
    hardwareToken: registration.hardwareToken
  };
  await saveDeviceCredentialsAtomically(credentials);
}

const session = await connection.invoke(
  "JoinCartGroup", credentials.cartId, credentials.hardwareToken);
```

`loadDeviceCredentials` ترجع `null` لجهاز لم يُجهّز فقط. لو الملف ناقص أو تالف، أو فشل الحفظ، أو رُفض التوكن عند الانضمام، يعرض الجهاز مشكلة تجهيز؛ لا يمسح البيانات ولا ينادي التسجيل تلقائيًا.
على reconnect يُعاد الانضمام بنفس الهوية، وليس إعادة تنفيذ التسجيل.

أخطاء التسجيل تصل كـSignalR invocation errors: `VALIDATION_FAILED` للطلب غير الصحيح، و`CART_ALREADY_JOINED` لو الاتصال مرتبط بالفعل بكارت.

## التسجيل اليدوي من الأدمن

`POST /api/v1/admin/carts` يتطلب JWT بدور `Admin`، باستخدام `Authorization: Bearer <admin-jwt>`.
الطلب بدون تسجيل دخول يرجع `401`، وطلبات `Customer` أو `Staff` ترجع `403`.

أرسل جسم JSON؛ `{}` كفاية. يمكن تحديد نسخة السوفتوير اختياريًا:

```json
{
  "swVersion": "cart-v1"
}
```

`swVersion` اختيارية، بحد أقصى 32 حرفًا بعد إزالة المسافات من الطرفين.
القيمة الفارغة تُحفظ `null`، والقيمة الأطول ترجع `400` بدون إنشاء كارت.

الـHTTP endpoint والـHub يناديان `ICartService.RegisterCartAsync` داخل `CartService` الموجودة، وتُحفظ الكارت عبر `CartRepository` الموجودة. الـHTTP ترجع `201 Created` بنفس بيانات رد `RegisterCart`:

```json
{
  "cartId": "e4617fac59cf4be0b3bc745cb5de95ae",
  "hardwareToken": "<device-secret-returned-once>",
  "status": "active",
  "swVersion": "cart-v1"
}
```

- السيرفر يولّد `cartId` ثابتًا بطول 32 حرفًا، وتوكن عشوائيًا مستقلًا لكل كارت بقوة 256 بت (64 حرف hexadecimal).
- قاعدة البيانات تحتفظ فقط بـSHA-256 hash للتوكن في `TokenHash`. لا يرجع الـhash في الاستجابة.
- التوكن الأصلي يظهر في رد الإنشاء فقط. الجهاز يحفظه في إعداداته المحلية، وليس في تطبيق العميل أو الكود المشترك. عند التسجيل اليدوي، سلّم البيانات لفريق الـhardware عبر قناة خاصة.
- استجابة HTTP تحمل `Cache-Control: no-store`. لا تسجل بيانات التجهيز أو رد SignalR في logs أو analytics.
- الكارت تبدأ مفعّلة، لكن هذا لا يعني أنها متصلة: `LastSeenAt` و`BatteryPct` يظلان `null` حتى يرسل الجهاز telemetry.
- الـID والتوكن من السيرفر؛ لا تحتاج إرسالهم في طلب الإنشاء. كل طلب إنشاء ناجح ينشئ كارت مستقلة. التسجيل ليس idempotent حاليًا: لو ضاع الرد بعد الحفظ على السيرفر أو فُقد ملف الهوية، إعادة التسجيل يمكن أن تنشئ كارت إضافية. لا توجد حاليًا عملية لاسترجاع التوكن أو تجديده، فلا تعمل retry تلقائيًا للتسجيل عند نتيجة غير معروفة.

## توصيل جهاز الـhardware

عند استخدام التسجيل اليدوي، جهّز الجهاز بعنوان السيرفر و`cartId` و`hardwareToken` من استجابة الأدمن، ثم اتبع خطوة الانضمام السابقة. إنشاء كارت من الأدمن لا ينقل بياناتها تلقائيًا للجهاز.

الـhardware token ليس JWT: يُرسل كوسيط إلى `JoinCartGroup`.
الرجوع بـ`null` يعني أن الجهاز انضم بنجاح ولا توجد جلسة مفتوحة. يمكن بعدها استدعاء `StartSession`.
يستخدم الجهاز نفس الـID والتوكن بعد إعادة التشغيل أو إعادة الاتصال؛ لا يسجّل كارت جديدة لكل جلسة.

التفاصيل وأسماء الأوامر والباراميترز والردود موجودة في [عقد الـCart Hub](cart-signalr.md).
سيناريوهات تحديث الجلسة موضحة في [الجلسات والتحديثات الحية](session-realtime.md).

## نطاق التغيير

هذه الإضافة لتسجيل الكارت فقط، باستخدام الحقول الموجودة بدون migration.
قائمة إدارة الكروت، وتجديد التوكن، وعمليات التفعيل والتعطيل للأدمن، وأحداث حالة الكارت والرفوف والداشبورد ليست ضمن هذه الإضافة.
