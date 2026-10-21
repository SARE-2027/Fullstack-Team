using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentValidation;
using SARE.Application.Common.Exceptions;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;
using SARE.Domain.Enums;

namespace SARE.Application.Services;

public sealed class CartMovementService(
    ISessionRepository sessions, ICartMovementRepository repository,
    IProductVariantRepository variants, ISessionService sessionService,
    ICartNotificationService notifications, IValidator<CartMovementRequest> validator,
    CartMovementProcessor processor) : ICartMovementService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // يستقبل تقرير حركة واحدة نهائية؛ إعادة الإرسال لا تعيد حساب الفاتورة.
    public async Task<CartMovementResult> ReportAsync(Guid sessionId, CartMovementRequest request, CancellationToken ct = default)
    {
        // نرفض المدخلات الناقصة والوزن غير المستقر قبل تغيير أي بيانات.
        await validator.ValidateAndThrowAsync(request, ct);
        // نوحّد دقة الوقت مع PostgreSQL قبل حساب بصمة الرسالة.
        request = request with { StartedAtUtc = Microseconds(request.StartedAtUtc), EndedAtUtc = Microseconds(request.EndedAtUtc) };
        // هذه البصمة تمنع استخدام نفس eventId مع محتوى مختلف.
        var payloadHash = Hash(request);
        // نبحث عن الإيصال أولًا لأن إعادة الإرسال تظل صحيحة حتى بعد إغلاق الجلسة.
        var receipt = await repository.GetReceiptAsync(request.EventId, ct);
        // وجود الإيصال يعني أن الرسالة اتعالجت بالفعل.
        if (receipt is not null)
            // نرجع الحالة المحفوظة بدون حفظ جديد أو إشعار جديد.
            return await ReplayAsync(receipt, sessionId, request.MovementId, payloadHash, ct);

        // الحركة الجديدة تحتاج جلسة مفتوحة ورقم الرسالة التالي بالضبط.
        var session = await RequireOpenSessionAsync(sessionId, request.Sequence, ct);
        // نفس الحركة برسالة مختلفة خطأ في العميل؛ لا نحفظها مرة ثانية.
        if (await repository.GetAsync(request.MovementId, ct) is not null)
            // الرفض لا يستهلك sequence ولا يغيّر الفاتورة.
            throw new ConflictException("Movement already recorded; retry its original eventId.", "MOVEMENT_ALREADY_RECORDED");

        // السيرفر ينشئ IDs البنود ويحفظ ترتيبها داخل الرسالة.
        var movement = CreateMovement(sessionId, request);
        // نقرأ بنود الفاتورة الحالية وأسعارها وأوزانها المحفوظة.
        var invoiceItems = await repository.GetSessionItemsAsync(sessionId, ct);
        // نجمع باركودات هذه الحركة فقط للحصول على بياناتها باستعلام واحد.
        var barcodes = request.Items.Select(item => item.Barcode).OfType<string>().Distinct().ToArray();
        // السعر والوزن للصنف الجديد يؤخذان من الكتالوج على السيرفر.
        var catalog = await variants.GetCartVariantsAsync(barcodes, ct);
        // نحتفظ بإمكانية إخراج قطعة اشتراها العميل حتى لو تغيّر باركودها أو تعطلت في الكتالوج.
        foreach (var item in invoiceItems.Where(item => item.RemovedAt == null && item.ExpectedWeightG.HasValue))
            // هذا البديل يخص قطعة موجودة فقط؛ IsAvailable=false يمنع بيع قطعة جديدة منه.
            catalog.TryAdd(item.Barcode, new(item.VariantId, item.Barcode, item.UnitPriceMinor, item.ExpectedWeightG!.Value, false));

        // هذه القائمة تحتوي فقط البنود الجديدة التي سيتم إدخالها في قاعدة البيانات.
        IReadOnlyList<SessionItem> newInvoiceItems = [];
        // لو حالة السلة السابقة غير محسومة، لا نفترض أن وزن البداية الحالي صحيح.
        if (movement.HasBasketChange && await repository.HasUnresolvedBasketMovementAsync(sessionId, ct))
            // نسجل الحركة للمشرف؛ لا نعيد تشغيل الحركات القديمة تلقائيًا.
            movement.ReasonCode = "MANUAL_REVIEW_REQUIRED";
        else
            // نخطط الحركة كاملة، ونطبقها على الفاتورة فقط إذا اجتازت كل الفحوص.
            newInvoiceItems = processor.Process(session, movement, invoiceItems, catalog);

        // نسجل أحدث وزن أبلغ عنه الجهاز حتى تظهر المشكلة للمشرف عند الاختلاف.
        if (movement.HasBasketChange)
        {
            // هذا وزن مُشاهد، وليس دليلًا أن كل القطع اتقبلت في الفاتورة.
            session.BasketWeightG = movement.WeightAfterG;
        }
        // تسجيل الحركة المقبولة أو المحتاجة مراجعة يستهلك رقم الرسالة مرة واحدة.
        session.LastMovementSequence = request.Sequence;
        // نحسب الإجمالي من البنود النشطة؛ الأسعار القديمة لا تتغير مع الكتالوج.
        var total = invoiceItems.Where(item => item.RemovedAt == null).Sum(item => (long)item.UnitPriceMinor);
        // نمنع تجاوز سعة عمود الإجمالي قبل تنفيذ الحفظ.
        if (total > int.MaxValue)
            // الطلب الفاشل هنا لا يسجل إيصالًا ولا ينشر نتيجة غير محفوظة.
            throw new ConflictException("Session total exceeds the supported amount.", "TOTAL_LIMIT_EXCEEDED");
        // نحفظ الإجمالي النهائي في الجلسة.
        session.TotalMinor = (int)total;
        // الإيصال يربط eventId بالحركة ومحتوى الرسالة ورقمها.
        receipt = Receipt(sessionId, movement.Id, request.EventId, request.Sequence, request.DeviceBootId, payloadHash);
        // الحفظ ذري ثم ننشر السيشن الكاملة للمشتركين.
        return await SaveAndPublishAsync(session, movement, receipt, newInvoiceItems, ct);
    }
    public async Task<CartSummaryResponse> SetRemovalRequestAsync(Guid sessionId, Guid itemId, bool requested, CancellationToken ct = default)
    {
        // مرجع الشاشة هو ID بند الفاتورة الذي رجّعه السيرفر سابقًا.
        var session = await sessions.GetByIdAsync(sessionId, ct) ?? throw new NotFoundException("Session not found.");
        // نبحث داخل نفس الجلسة حتى لا يستطيع الطلب الإشارة لمشتريات جلسة أخرى.
        var items = await repository.GetSessionItemsAsync(sessionId, ct);
        // عدم وجود البند خطأ مرجع، وليس طلب إنشاء بند جديد.
        var item = items.SingleOrDefault(x => x.Id == itemId) ?? throw new NotFoundException("Session item not found.");
        // الضغط المكرر أو وصول الضغط بعد الخروج لا يخصم شيئًا ثانية.
        if (item.RemovedAt.HasValue || item.RemovalRequestedAt.HasValue == requested)
            return await sessionService.GetSessionSummaryAsync(sessionId, ct);
        // لا نقبل طلب إزالة جديد لجلسة انتهت.
        if (session.Status != SessionStatus.Open) throw new ConflictException("Session is closed.");
        // نسجل نية الإزالة فقط؛ الفاتورة تتغير مع الخروج الحقيقي.
        item.RemovalRequestedAt = requested ? DateTime.UtcNow : null;
        // تحديث الجلسة يحدّث Version ويحفظ علامة البند في نفس SaveChanges.
        await sessions.UpdateAsync(session, ct);
        // نقرأ الحالة المحفوظة التي ستظهر لكل المشتركين.
        var summary = await sessionService.GetSessionSummaryAsync(sessionId, ct);
        // الإشعار بعد الحفظ، بنفس DTO المستخدمة للقراءة والرد.
        await notifications.NotifySessionUpdatedAsync(summary, ct);
        // يرجع الرد لصاحب الـinvoke أو طلب HTTP.
        return summary;
    }

    public async Task<CartMovementResult> ReconcileAsync(Guid sessionId, ReconcileBasketRequest request, CancellationToken ct = default)
    {
        // الـHub يسمح بهذه العملية للأدمن فقط؛ هنا نتحقق من مدخلات قراره.
        if (request.EventId == Guid.Empty || request.MovementId == Guid.Empty || request.DeviceBootId == Guid.Empty
            || request.Sequence is < 1 or >= int.MaxValue
            || request.StableBasketWeightG is < 0 or > 1_000_000 || request.PendingMovementIds is null)
            throw new ConflictException("Valid IDs, stable weight and explicit pending movement IDs are required.", "VALIDATION_FAILED");
        // نفس قرار التسوية يمكن أن يُعاد بسبب انقطاع الشبكة، بدون تنفيذه مرتين.
        var hash = Hash(request);
        // نبحث عن نتيجة قرار المشرف المحفوظة قبل التحقق من حالة الجلسة.
        var existing = await repository.GetReceiptAsync(request.EventId, ct);
        // الإيصال الموجود يرد بالحالة الحالية ولا يكرر الإشعار.
        if (existing is not null) return await ReplayAsync(existing, sessionId, request.MovementId, hash, ct);
        // القرار الجديد يحتاج جلسة مفتوحة وsequence صحيحًا.
        var session = await RequireOpenSessionAsync(sessionId, request.Sequence, ct);
        // لا نعيد استخدام معرف حركة سابقة لقرار إداري جديد.
        if (await repository.GetAsync(request.MovementId, ct) is not null)
            throw new ConflictException("Use a new movementId for reconciliation.", "MOVEMENT_ID_REUSED");
        // هنا فقط نقرأ الحركات المعلقة: المشرف يراجعها، ولا نعيد تشغيل معالجتها.
        var pending = await repository.GetUnresolvedAsync(sessionId, ct);
        // يجب أن يكون المشرف شاهد نفس قائمة المشاكل الحالية قبل حسمها.
        if (!pending.Select(x => x.Id).Order().SequenceEqual(request.PendingMovementIds.Distinct().Order()))
            throw new ConflictException("Refresh and explicitly include every unresolved movement.", "PENDING_MOVEMENTS_CHANGED");
        // نقرأ الفاتورة المعتمدة بالفعل، وليس تخمينات الحركات غير المقبولة.
        var items = await repository.GetSessionItemsAsync(sessionId, ct);
        // القطع الممسوحة والمحمولة خارج السلة لا تدخل في وزنها.
        var inside = items.Where(x => x.RemovedAt == null && x.BasketPresence == BasketPresence.Inside).ToList();
        // الحقول المحفوظة تسمح ببيانات تاريخية ناقصة؛ لا نخترع وزنًا لها من الكتالوج الحالي.
        if (inside.Any(x => !x.ExpectedWeightG.HasValue)) throw new ConflictException("Historical weight snapshots are missing.");
        // هذا وزن المحتويات المعروفة التي أعادها المشرف للحالة السليمة فعليًا.
        var expected = inside.Sum(x => (long)x.ExpectedWeightG!.Value);
        // الوزن وحده لا يحدد الهوية؛ هو تحقق إضافي بعد مراجعة المشرف للقطع.
        if (Math.Abs(request.StableBasketWeightG - expected) > processor.Tolerance(inside.Select(x => x.ExpectedWeightG)))
            throw new ConflictException("Restore the known basket contents before reconciliation.", "BASKET_WEIGHT_MISMATCH");

        // نغلق المشاكل التي راجعها المشرف؛ لا نضيف محتوياتها تلقائيًا إلى الفاتورة.
        foreach (var entry in pending)
        {
            CancelUnresolved(entry, "CANCELLED_BY_RECONCILIATION");
        }
        // وقت قرار المشرف من ساعة السيرفر.
        var now = DateTime.UtcNow;
        // نسجل قرار المشرف كعملية إدارية واضحة، بدون بنود شراء جديدة.
        var movement = new CartMovement { Id = request.MovementId, SessionId = sessionId,
            Sequence = request.Sequence, DeviceBootId = request.DeviceBootId,
            StartedAtUtc = now, EndedAtUtc = now, WeightBeforeG = request.StableBasketWeightG,
            WeightAfterG = request.StableBasketWeightG,
            Status = CartMovementStatus.Accepted };
        // الحالة المشاهدة والمعتمدة أصبحتا نفس الوزن الذي راجعه المشرف.
        session.BasketWeightG = session.ReconciledBasketWeightG = request.StableBasketWeightG;
        // قرار المشرف رسالة جديدة في تسلسل الجلسة.
        session.LastMovementSequence = request.Sequence;
        // حفظ القرار وإغلاق المشاكل ثم الإشعار في نفس مسار الحفظ المعتاد.
        return await SaveAndPublishAsync(session, movement,
            Receipt(sessionId, movement.Id, request.EventId, request.Sequence, request.DeviceBootId, hash), [], ct);
    }                               

    // نبني سجلًا واحدًا من الرسالة النهائية؛ لا يوجد تعديل أو استبدال للحركة.
    private static CartMovement CreateMovement(Guid sessionId, CartMovementRequest request) => new()
    {
        // معرف العملية يأتي من الرسالة، ومعرف الجلسة سبق التحقق من ملكيته في الـHub/HTTP.
        Id = request.MovementId, SessionId = sessionId, Sequence = request.Sequence,
        // بيانات الجهاز والوقت توثق متى شاهد الحركة.
        DeviceBootId = request.DeviceBootId, StartedAtUtc = request.StartedAtUtc, EndedAtUtc = request.EndedAtUtc,
        // الوزن يخص السلة كلها؛ السكان وحده لا يحتاج وزنًا.
        WeightBeforeG = request.Weight?.BeforeG, WeightAfterG = request.Weight?.AfterG,
        // السماحية النهائية لا يمكن أن تتجاوز إعدادات السيرفر.
        WeightToleranceG = request.Weight?.ToleranceG ?? 0, HasBasketChange = request.Weight is not null,
        // نسخة الموديل معلومة تشخيصية عند إرسالها.
        ModelVersion = request.ModelVersion,
        // نحافظ على ترتيب الملاحظات الأصلي لعرض تقرير الحركة كما أرسله الجهاز.
        Items = request.Items.Select((input, position) => new CartMovementItem
        {
            // هذه IDs قاعدة البيانات؛ السيرفر يولدها، وليس الـPi.
            Id = Guid.NewGuid(), MovementId = request.MovementId, Position = position,
            // ننقل الملاحظات الأصلية كما وصلت، ثم نسجل نتيجة التحقق في حقول منفصلة.
            Action = input.Action, Source = input.Source, DetectedBarcode = input.Barcode,
            // الثقة والتتبع يخصان القطعة المرصودة.
            Confidence = input.Confidence, TrackId = input.TrackId, CameraId = input.CameraId,
            // اختيار بند للإخراج أو إلغاء السكان فقط؛ الدخول والإضافة لا يحملان مراجع بنود.
            TargetSessionItemId = input.SessionItemId
        }).ToList()
    };
    private static void CancelUnresolved(CartMovement movement, string reason)
    {
        // الحسم اليدوي يغيّر النتيجة فقط، ويحافظ على الباركود والثقة والقراءات الأصلية.
        foreach (var item in movement.Items.Where(item => item.Outcome == CartItemOutcome.Unknown))
        {
            // إغلاق الملاحظة غير المقبولة لا يحذف الأدلة الأصلية.
            item.Outcome = CartItemOutcome.Rejected;
            // السبب يوضح أن المشرف حسم الحالة، وليس أن الكاميرا عدلت نفسها.
            item.ReasonCode = reason;
        }
        // لا تعود الحركة الملغاة إلى المعالجة عند وصول رسالة أخرى.
        movement.Status = CartMovementStatus.Cancelled;
        // الواجهة تعرض سبب الإلغاء عند قراءة أو إعادة إرسال الحركة.
        movement.ReasonCode = reason;
    }
    private async Task<Session> RequireOpenSessionAsync(Guid sessionId, int sequence, CancellationToken ct)
    {
        // الجلسة هي صاحب الفاتورة والتسلسل، ولا نثق في معرّف غير موجود.
        var session = await sessions.GetByIdAsync(sessionId, ct) ?? throw new NotFoundException("Session not found.");
        // الحركة الجديدة لا تدخل على فاتورة مقفولة.
        if (session.Status != SessionStatus.Open) throw new ConflictException("Session is closed.");
        // نرفض الفجوة بدل تطبيق حركة قبل الرسالة السابقة؛ الجهاز يعيد الإرسال بالترتيب.
        if (sequence != session.LastMovementSequence + 1)
            throw new ConflictException($"Expected sequence {session.LastMovementSequence + 1}.", "SEQUENCE_GAP");
        return session;
    }

    private async Task<CartMovementResult> SaveAndPublishAsync(Session session, CartMovement movement,
        CartEventReceipt receipt, IReadOnlyList<SessionItem> additions, CancellationToken ct)
    {
        // الحركة والبنود والفاتورة والإيصال تحفظ معًا؛ لا يظهر للمستخدم نصف عملية.
        if (!await repository.TrySaveAsync(session, movement, receipt, additions, ct))
        {
            // طلب مطابق سبق هذا الطلب أثناء الحفظ؛ نقرأ نتيجة الطلب الذي نجح.
            var winner = await repository.GetReceiptAsync(receipt.Id, ct) ?? throw new ConflictException("Retry the same eventId.");
            return await ReplayAsync(winner, session.Id, movement.Id, receipt.PayloadHash, ct);
        }
        // هذه القراءة بعد نجاح الحفظ الفعلي، فتطابق ما في قاعدة البيانات.
        var result = await ResultAsync(receipt.Id, movement, ct);
        // نرسل السيشن الكاملة فقط بعد الحفظ.
        await notifications.NotifySessionUpdatedAsync(result.Session, ct);
        return result;
    }

    private async Task<CartMovementResult> ReplayAsync(CartEventReceipt receipt, Guid sessionId, Guid movementId, string hash, CancellationToken ct)
    {
        // نفس eventId لا يجوز استخدامه لجلسة أو حركة أو محتوى مختلف.
        if (receipt.SessionId != sessionId || receipt.MovementId != movementId || receipt.PayloadHash != hash)
            throw new ConflictException("eventId was used for another message.", "EVENT_ID_REUSED");
        // نرجع الحركة المحفوظة والسيشن الحالية، حتى لو الجلسة اتقفلت بعد تنفيذها.
        return await ResultAsync(receipt.Id, await repository.GetAsync(movementId, ct)
            ?? throw new ConflictException("Movement is unavailable."), ct);
    }

    private async Task<CartMovementResult> ResultAsync(Guid eventId, CartMovement movement, CancellationToken ct)
    {
        // نفس عقد القراءة يستخدمه HTTP وSignalR والرد المباشر.
        var summary = await sessionService.GetSessionSummaryAsync(movement.SessionId, ct);
        // نخبر الجهاز بالرقم التالي مع نتيجة الحركة والسيشن الحالية الكاملة.
        return new(eventId, summary.LastMovementSequence + 1, ToResponse(movement), summary);
    }

    // تحويل للعرض فقط؛ IDs البنود هنا نتائج أنشأها السيرفر وليست مدخلات من الجهاز.
    public static CartMovementResponse ToResponse(CartMovement movement)
    {
        // نرجع نفس ترتيب الملاحظات الذي أرسله الجهاز.
        var observations = movement.Items.OrderBy(item => item.Position).Select(item => new CartMovementItemResponse(
            // هوية السجل، العملية، والباركود المرصود.
            item.Id, item.Action.ToString(), item.DetectedBarcode, item.VariantId,
            // بند الفاتورة يتحدد فقط بعد نجاح المعالجة، مع نتيجة الملاحظة وأثرها.
            item.SessionItemId, item.Outcome.ToString(), item.Effect.ToString(), item.ReasonCode,
            // بيانات المصدر والربط والتتبع التي يستخدمها الجهاز والواجهة.
            item.Source, item.Confidence, item.TrackId, item.CameraId,
            // السعر والوزن من السيرفر؛ قد يكونان مرشحين فقط عند NeedsConfirmation.
            item.ExpectedWeightG, item.UnitPriceMinor)
        {
            // المرجع الذي طلبه العميل يختلف عن بند الفاتورة الذي اعتمدناه فعليًا.
            TargetSessionItemId = item.TargetSessionItemId,
            // رقم ترتيب الملاحظة داخل الرسالة يبدأ من صفر.
            Position = item.Position
        }).ToList();
        // نرجع قرار الحركة مع وزنها وكل ملاحظاتها.
        return new(movement.Id, movement.Status.ToString(), movement.ReasonCode,
            // القراءات تخص السلة كلها، وليست وزنًا لكل عنصر في الطلب.
            movement.WeightBeforeG, movement.WeightAfterG, observations)
        {
            // رقم الحركة هو نفس رقم رسالتها الأصلية؛ لا يوجد ترتيب تصحيح آخر.
            Sequence = movement.Sequence,
            // التتبع مقيد بتشغيل الجهاز الذي شاهد الحركة.
            DeviceBootId = movement.DeviceBootId,
            // وقت بداية الحركة كما أبلغه الجهاز.
            StartedAtUtc = movement.StartedAtUtc,
            // وقت نهاية الحركة المكتملة.
            EndedAtUtc = movement.EndedAtUtc,
            // نسخة الموديل للتشخيص عند الحاجة.
            ModelVersion = movement.ModelVersion
        };
    }

    // الإيصال يثبت نجاح تسجيل رسالة معينة، ولا يحتوي نسخة JSON ثانية من البيزنس.
    private static CartEventReceipt Receipt(Guid sessionId, Guid movementId, Guid eventId, int sequence, Guid bootId, string hash) => new()
    {
        // eventId هو مفتاح منع تكرار الرسالة على الشبكة.
        Id = eventId,
        // الربط بالجلسة يمنع إعادة استخدام الرسالة في فاتورة أخرى.
        SessionId = sessionId,
        // الربط بالحركة يعيد النتيجة الصحيحة عند Retry.
        MovementId = movementId,
        // الفهرس الفريد على الجلسة والرقم يمنع تعارض رسالتين على نفس الترتيب.
        Sequence = sequence,
        // نسجل تشغيل الجهاز لأغراض التتبع.
        DeviceBootId = bootId,
        // البصمة تكشف تغيير المحتوى عند إعادة استخدام eventId.
        PayloadHash = hash,
        // وقت استلام السيرفر مستقل عن ساعة الجهاز.
        ReceivedAtUtc = DateTime.UtcNow
    };

    // البصمة للمقارنة ومنع إعادة استخدام المعرف مع محتوى آخر، وليست تشفيرًا للبيانات.
    private static string Hash<T>(T value)
    {
        // نحول نفس DTO دائمًا بنفس إعدادات أسماء JSON.
        var json = JsonSerializer.Serialize(value, JsonOptions);
        // SHA256 يعمل على بايتات ثابتة بترميز UTF8.
        var bytes = Encoding.UTF8.GetBytes(json);
        // نحتفظ بالبصمة فقط في الإيصال.
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    // PostgreSQL يحفظ microseconds؛ توحيد الدقة يمنع اختلاف البصمة بسبب كسور ticks الزائدة.
    private static DateTime Microseconds(DateTime value) => value.AddTicks(-(value.Ticks % 10));
}
