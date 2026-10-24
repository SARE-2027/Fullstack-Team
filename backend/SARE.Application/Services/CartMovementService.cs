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

public sealed partial class CartMovementService(
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
        // أثناء انتظار الموظف أو الجرد لا تمر الملاحظات بمحاسبة التسوق العادية.
        if (session.BasketState is BasketState.NeedsStaff or BasketState.Auditing || request.AuditId.HasValue
            || session.LastAuditCompletedAtUtc.HasValue && request.StartedAtUtc <= session.LastAuditCompletedAtUtc.Value)
            return await ReportReviewAsync(session, request, payloadHash, ct);
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
            catalog.TryAdd(item.Barcode, new(item.VariantId, item.Barcode, item.UnitPriceMinor, item.ExpectedWeightG!.Value, false)
                { WeightTolerancePercent = item.WeightTolerancePercent });

        // نخصم كتلة الخروج المعلق من الوجود الفعلي، مع تأجيل خصم الفاتورة لحسم الطابور.
        var unresolved = await repository.GetUnresolvedAsync(sessionId, ct);
        var newInvoiceItems = processor.Process(session, movement, invoiceItems, catalog,
            unresolved);
        if (CartExitQueue.IsOpen(movement)) unresolved.Add(movement);

        // نسجل أحدث وزن أبلغ عنه الجهاز حتى تظهر المشكلة للمشرف عند الاختلاف.
        if (movement.HasBasketChange)
        {
            // هذا وزن مُشاهد، وليس دليلًا أن كل القطع اتقبلت في الفاتورة.
            session.BasketWeightG = movement.WeightAfterG;
        }
        ResolveExitQueue(session, unresolved, invoiceItems, movement);
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
    // القرار يخص خروجًا محفوظًا؛ لا نطلب سكانًا جديدًا ولا نعيد تطبيق التقرير الأصلي.
    public async Task<CartMovementResult> ConfirmExitAsync(Guid sessionId, ConfirmExitChoiceRequest request, CancellationToken ct = default)
    {
        if (request.EventId == Guid.Empty || request.MovementId == Guid.Empty || request.DeviceBootId == Guid.Empty
            || request.Sequence is < 1 or >= int.MaxValue || string.IsNullOrWhiteSpace(request.ExpectedVersion)
            || request.SessionItemIds is null || request.SessionItemIds.Count > 500
            || request.SessionItemIds.Distinct().Count() != request.SessionItemIds.Count)
            throw new ConflictException("Valid command metadata and distinct invoice IDs are required.", "VALIDATION_FAILED");
        var hash = Hash(request);
        var receipt = await repository.GetReceiptAsync(request.EventId, ct);
        if (receipt is not null) return await ReplayAsync(receipt, sessionId, request.MovementId, hash, ct);
        var session = await RequireOpenSessionAsync(sessionId, request.Sequence, ct);
        if (session.LastActivityAt.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture) != request.ExpectedVersion)
            throw new ConflictException("Refresh the session before confirming this choice.", "SESSION_VERSION_CHANGED");
        if (await repository.GetAsync(request.MovementId, ct) is not null)
            throw new ConflictException("Retry the original eventId.", "MOVEMENT_ALREADY_RECORDED");
        var report = await repository.GetAsync(request.PendingMovementId, ct);
        if (session.BasketState != BasketState.Pending || report is null || report.SessionId != sessionId
            || report.Status != CartMovementStatus.NeedsConfirmation || !report.UnconfirmedExitWeightG.HasValue)
            throw new ConflictException("Refresh the pending exit.", "PENDING_MOVEMENT_CHANGED");
        var unresolved = await repository.GetUnresolvedAsync(sessionId, ct);
        if (CartExitQueue.First(unresolved)?.Id != report.Id)
            throw new ConflictException("Answer the oldest exit question first.", "EXIT_ORDER_REQUIRED");
        var invoice = await repository.GetSessionItemsAsync(sessionId, ct);
        var selected = invoice.Where(item => request.SessionItemIds.Contains(item.Id)).ToList();
        if (selected.Count != request.SessionItemIds.Count || selected.Any(item => item.RemovedAt.HasValue
            || item.BasketPresence != BasketPresence.Inside || !item.ExpectedWeightG.HasValue
            || item.PlacementSequence > report.Sequence))
            throw new ConflictException("Select current Inside units from this session.", "INVALID_ITEM_SELECTION");

        var now = DateTime.UtcNow;
        var decision = new CartMovement
        {
            Id = request.MovementId,
            SessionId = sessionId,
            Sequence = request.Sequence,
            DeviceBootId = request.DeviceBootId,
            StartedAtUtc = now,
            EndedAtUtc = now,
            RelatedMovementId = report.Id,
            ConfirmedByCustomer = false,
            Status = CartMovementStatus.Accepted,
            PendingWeightBeforeG = session.PendingWeightG
        };
        var selectedRange = CartWeightPolicy.Sum(selected.Select(CartMovementProcessor.UnitRange));
        var possibleExit = selectedRange + new CartWeightRange(0, report.ExitPendingWeightG > 0
            ? (long)report.ExitPendingWeightG + report.ExitPendingUncertaintyG : 0);
        if (request.NeedsStaff || !CartExitQueue.SelectionMatchesIdentification(selected, report)
            || !CartExitQueue.SelectionLeavesLaterEvidence(invoice, report, selected, unresolved)
            || !possibleExit.Expand(report.ExitWeightUncertaintyG)
            .Contains(report.UnconfirmedExitWeightG.Value))
        {
            // اختيار متناقض لا يخصم شيئًا، لكنه يُحفظ كطلب مراجعة بدل فقد قرار العميل.
            session.BasketState = BasketState.NeedsStaff;
            session.StaffRequestedAt ??= now;
            session.StaffRequestReason = request.NeedsStaff ? "CUSTOMER_REQUESTED_HELP" : "EXIT_CHOICE_WEIGHT_MISMATCH";
            decision.ReasonCode = request.NeedsStaff ? "CUSTOMER_REQUESTED_HELP" : "EXIT_CHOICE_WEIGHT_MISMATCH";
        }
        else
        {
            decision.ConfirmedByCustomer = true;
            SettleExit(session, report, selected, unresolved, decision, "SETTLED_BY_CUSTOMER");
            ResolveExitQueue(session, unresolved, invoice, decision, allowUniqueWithPending: true);
            decision.ReasonCode ??= "SETTLED_BY_CUSTOMER";
        }
        session.TotalMinor = checked((int)invoice.Where(item => item.RemovedAt == null).Sum(item => (long)item.UnitPriceMinor));
        session.LastMovementSequence = request.Sequence;
        return await SaveAndPublishAsync(session, decision,
            Receipt(sessionId, decision.Id, request.EventId, request.Sequence, request.DeviceBootId, hash), [], ct);
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
        if (session.BasketState is BasketState.NeedsStaff or BasketState.Auditing)
            throw new ConflictException("Basket is under staff review.", "STAFF_REVIEW_ACTIVE");
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

    // نبني سجلًا واحدًا من الرسالة النهائية؛ لا يوجد تعديل أو استبدال للحركة.
    private static CartMovement CreateMovement(Guid sessionId, CartMovementRequest request) => new()
    {
        // معرف العملية يأتي من الرسالة، ومعرف الجلسة سبق التحقق من ملكيته في الـHub/HTTP.
        Id = request.MovementId,
        SessionId = sessionId,
        Sequence = request.Sequence,
        // بيانات الجهاز والوقت توثق متى شاهد الحركة.
        DeviceBootId = request.DeviceBootId,
        StartedAtUtc = request.StartedAtUtc,
        EndedAtUtc = request.EndedAtUtc,
        // الوزن يخص السلة كلها؛ السكان وحده لا يحتاج وزنًا.
        WeightBeforeG = request.Weight?.BeforeG,
        WeightAfterG = request.Weight?.AfterG,
        HasBasketChange = request.Weight is not null,
        // نسخة الموديل معلومة تشخيصية عند إرسالها.
        ModelVersion = request.ModelVersion,
        // نحافظ على ترتيب الملاحظات الأصلي لعرض تقرير الحركة كما أرسله الجهاز.
        Items = request.Items.Select((input, position) => new CartMovementItem
        {
            // هذه IDs قاعدة البيانات؛ السيرفر يولدها، وليس الـPi.
            Id = Guid.NewGuid(),
            MovementId = request.MovementId,
            Position = position,
            // ننقل الملاحظات الأصلية كما وصلت، ثم نسجل نتيجة التحقق في حقول منفصلة.
            Action = input.Action,
            Source = input.Source,
            DetectedBarcode = input.Barcode,
            // الثقة والتتبع يخصان القطعة المرصودة.
            Confidence = input.Confidence,
            TrackId = input.TrackId,
            CameraId = input.CameraId,
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
        movement.PendingWeightAfterG = session.PendingWeightG;
        if (session.BasketState == BasketState.NeedsStaff)
        {
            session.StaffRequestedAt ??= DateTime.UtcNow;
            session.StaffRequestReason ??= movement.ReasonCode ?? "STAFF_REVIEW_REQUIRED";
        }
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
            ModelVersion = movement.ModelVersion,
            PendingWeightBeforeG = movement.PendingWeightBeforeG,
            PendingWeightAfterG = movement.PendingWeightAfterG,
            UnconfirmedExitWeightG = movement.UnconfirmedExitWeightG,
            RelatedMovementId = movement.RelatedMovementId,
            ResolvedByMovementId = movement.ResolvedByMovementId,
            ConfirmedByCustomer = movement.ConfirmedByCustomer,
            AuditId = movement.AuditId
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
