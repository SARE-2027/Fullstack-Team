using Microsoft.Extensions.Options;
using SARE.Application.Common;
using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;
using SARE.Domain.Enums;

namespace SARE.Application.Services;

// يراجع حركة واحدة فقط؛ لا يستقبل أجزاء حركة ولا يعيد معالجة حركات قديمة.
public sealed class CartMovementProcessor(IOptions<CartMovementOptions> options)
{
    // قرار مؤقت في الذاكرة؛ لا نغيّر بنود الفاتورة قبل اكتمال كل الفحوص.
    private sealed record ItemChange(CartMovementItem Observation, SessionItem? Unit, CartItemEffect Effect);

    // يرجع البنود الجديدة التي ستُحفظ، أو قائمة فارغة إذا احتاجت الحركة مراجعة.
    public IReadOnlyList<SessionItem> Process(Session session, CartMovement movement,
        List<SessionItem> invoiceItems, Dictionary<string, CartVariantData> catalog)
    {
        // نحدد بيانات كل الملاحظات ليراها المشرف حتى لو فشلت ملاحظة أخرى.
        foreach (var observation in movement.Items)
            Identify(observation, catalog);
        // المجهول أو الثقة المنخفضة يمنعان اعتماد الحركة كاملة.
        var unidentified = movement.Items.FirstOrDefault(item => item.ReasonCode is not null);
        // نتيجة غير محسومة لا تضيف ولا تحذف أي بند في الفاتورة.
        if (unidentified is not null)
            return NeedsReview(movement, unidentified.ReasonCode!);

        // البنود النشطة هي المشتريات الحالية، بما فيها القطع الممسوحة خارج السلة.
        var activeUnits = invoiceItems.Where(item => item.RemovedAt == null).ToList();
        // الوزن يخص الموجود داخل السلة فقط.
        var insideUnits = activeUnits.Where(item => item.BasketPresence == BasketPresence.Inside).ToList();
        // هذه البنود الجديدة لم تدخل قاعدة البيانات بعد.
        var newUnits = new List<SessionItem>();
        // القرارات ستطبق كلها معًا بعد التحقق من الهوية والوزن.
        var changes = new List<ItemChange>();

        // نجهّز السكان أولًا حتى نطابق الدخول معه تلقائيًا، حتى داخل نفس الرسالة.
        foreach (var scan in movement.Items.Where(item => item.Action == CartItemAction.Scan))
        {
            // Scanner يحدد الباركود؛ السعر والوزن ما زالا من الكتالوج.
            var variant = catalog[scan.DetectedBarcode!];
            // لا نبيع صنفًا معطلًا أو بديلًا تاريخيًا من الفاتورة.
            if (!variant.IsAvailable)
                return NeedsReview(movement, "PRODUCT_UNAVAILABLE");
            // كل سكان إضافة جديدة؛ السيرفر يحفظ نسخة السعر والوزن ويولّد ID البند.
            var unit = NewUnit(session.Id, scan, variant, BasketPresence.Unconfirmed);
            // سنعمل INSERT لهذا البند إذا نجحت الحركة كاملة.
            newUnits.Add(unit);
            // السكان يزيد الفاتورة مرة واحدة، والدخول لاحقًا يؤكد الوضع فقط.
            changes.Add(new(scan, unit, CartItemEffect.Added));
        }

        // لا نربط الدخول تلقائيًا بقطعة اختار العميل إلغاء سكانها في نفس الحركة.
        var cancelledScanIds = movement.Items.Where(item => item.Action == CartItemAction.CancelScan)
            .Select(item => item.TargetSessionItemId!.Value).ToHashSet();
        // نرتب المشتريات السابقة بالأقدم، ثم نضيف سكانات هذه الحركة بترتيب ورودها.
        var unplacedScans = activeUnits.OrderBy(unit => unit.AddedAt).ThenBy(unit => unit.Id).Concat(newUnits)
            .Where(unit => unit.Source == CartItemSource.Scanner && unit.BasketPresence == BasketPresence.Unconfirmed
                && !cancelledScanIds.Contains(unit.Id)).ToList();

        // القطعة التي دخلت وخرجت بنفس التتبع داخل نفس الحركة لم تغيّر المشتريات.
        var roundTrips = FindRoundTrips(movement.Items);
        // نسجل عدم التغيير بدل إنشاء بند ثم حذفه.
        changes.AddRange(roundTrips.Select(item => new ItemChange(item, null, CartItemEffect.NoChange)));
        // نراجع الدخول والخروج الفعليين فقط.
        foreach (var observation in movement.Items.Where(item => item.Action is CartItemAction.Enter or CartItemAction.Exit).Except(roundTrips))
        {
            // الخروج يجب أن يطابق قطعة موجودة فعلًا داخل السلة.
            if (observation.Action == CartItemAction.Exit)
            {
                // نفس بند الفاتورة لا يمكن إخراجه مرتين داخل الرسالة.
                var candidates = insideUnits.Where(unit => Matches(unit, observation)
                    && changes.All(change => change.Effect != CartItemEffect.Removed || change.Unit != unit)).ToList();
                // نختار بالمرجع أو التتبع؛ القطع ذات السعر/الوزن المختلف تحتاج اختيارًا صريحًا.
                var selected = SelectExit(observation, movement, candidates);
                // لا نخمن أي بند مالي يجب خصمه عند الالتباس.
                if (selected is null)
                    return NeedsReview(movement, "SELECT_SESSION_ITEM");
                // نجهّز الحذف، بدون تعديل RemovedAt قبل مراجعة الوزن.
                changes.Add(new(observation, selected, CartItemEffect.Removed));
                // انتهى التعامل مع ملاحظة الخروج الحالية.
                continue;
            }

            // الدخول يستهلك تلقائيًا أقدم سكان منتظر من نفس الباركود في هذه الجلسة.
            var placed = unplacedScans.FirstOrDefault(unit => Matches(unit, observation));
            // وجود قطعة مستنية يؤكد وضعها بدون إضافة سعرها مرة ثانية.
            if (placed is not null)
            {
                // تأكيد الوضع يغيّر حالة الوجود داخل السلة فقط.
                changes.Add(new(observation, placed, CartItemEffect.PlacementConfirmed));
                // نحجزها في التخطيط فورًا حتى لا يؤكد دخولان نفس القطعة داخل الحركة.
                unplacedScans.Remove(placed);
            }
            else
            {
                // نقرأ بيانات البيع فقط لو الدخول يمثل قطعة جديدة.
                var variant = catalog[observation.DetectedBarcode!];
                // إضافة قطعة جديدة تتطلب صنفًا متاحًا للبيع.
                if (!variant.IsAvailable)
                    return NeedsReview(movement, "PRODUCT_UNAVAILABLE");
                // نجهّز قطعة جديدة داخل السلة، بسعر ووزن السيرفر.
                var unit = NewUnit(session.Id, observation, variant, BasketPresence.Inside);
                // هذا البند سيُدرج فقط بعد نجاح وزن الحركة كلها.
                newUnits.Add(unit);
                // نسجل الإضافة المخطط لها.
                changes.Add(new(observation, unit, CartItemEffect.Added));
            }
        }

        // إلغاء السكان يخص قطعة لم توضع في السلة، أو قطعة خرجت بالفعل.
        foreach (var cancellation in movement.Items.Where(item => item.Action == CartItemAction.CancelScan))
        {
            // المرجع هنا ID سابق من السيرفر، وليس ID جديدًا ينشئه الجهاز.
            var unit = invoiceItems.SingleOrDefault(item => item.Id == cancellation.TargetSessionItemId);
            // البند يجب أن يخص نفس الصنف والجلسة.
            if (unit is null || !Matches(unit, cancellation))
                return NeedsReview(movement, "ITEM_NOT_IN_SESSION");
            // إذا كان الخروج مسجلًا فلا نخصم السعر ثانية.
            if (unit.RemovedAt.HasValue || changes.Any(change => change.Unit == unit && change.Effect == CartItemEffect.Removed))
                changes.Add(new(cancellation, unit, CartItemEffect.NoChange));
            else
            {
                // زر الإلغاء لا يسمح بإسقاط سعر قطعة ما زالت داخل السلة.
                if (unit.BasketPresence != BasketPresence.Unconfirmed
                    || changes.Any(change => change.Unit == unit && change.Effect == CartItemEffect.PlacementConfirmed))
                    return NeedsReview(movement, "PHYSICAL_EXIT_REQUIRED");
                // نجهّز إلغاء القطعة الممسوحة الموجودة خارج السلة.
                changes.Add(new(cancellation, unit, CartItemEffect.Removed));
            }
        }

        // السكان وحده لا يحتاج ميزانًا؛ الحركات الفيزيائية تراجع وزن السلة بالكامل.
        if (movement.HasBasketChange)
        {
            // هذه أوزان بنود الفاتورة المحفوظة، وليست أوزانًا مرسلة من الجهاز.
            if (insideUnits.Any(unit => !unit.ExpectedWeightG.HasValue)
                || changes.Any(change => change.Observation.Action == CartItemAction.Enter
                    && change.Unit is not null && !change.Unit.ExpectedWeightG.HasValue))
                return NeedsReview(movement, "WEIGHT_SNAPSHOT_MISSING");
            // نعرف وزن البداية من القطع المؤكدة داخل السلة.
            var expectedBefore = insideUnits.Sum(unit => (long)unit.ExpectedWeightG!.Value);
            // نحسب الدخول والخروج؛ السكان والإلغاء خارج السلة لا يغيران وزنها.
            var expectedDelta = changes.Sum(change => PhysicalWeight(change));
            // السماحية محدودة بإعدادات السيرفر وبوزن أصغر قطعة.
            var weights = insideUnits.Select(unit => unit.ExpectedWeightG).Concat(changes
                .Where(change => change.Observation.Action == CartItemAction.Enter).Select(change => change.Unit?.ExpectedWeightG));
            var tolerance = Tolerance(weights, movement.WeightToleranceG);
            // قراءة البداية يجب أن تتصل بآخر حالة سليمة للسلة.
            if (Math.Abs(movement.WeightBeforeG!.Value - (session.ReconciledBasketWeightG ?? expectedBefore)) > tolerance)
                return NeedsReview(movement, "WEIGHT_CONTINUITY_MISMATCH");
            // وزن البداية يجب أن يوافق محتويات السلة المعروفة.
            if (Math.Abs(movement.WeightBeforeG.Value - expectedBefore) > tolerance)
                return NeedsReview(movement, "BASKET_WEIGHT_MISMATCH");
            // فرق الميزان يجب أن يطابق مجموع ما دخل ناقص ما خرج.
            if (Math.Abs((long)movement.WeightAfterG!.Value - movement.WeightBeforeG.Value - expectedDelta) > tolerance)
                return NeedsReview(movement, "MOVEMENT_WEIGHT_MISMATCH");
            // الوزن النهائي يجب أن يوافق المحتويات النهائية أيضًا.
            if (Math.Abs(movement.WeightAfterG.Value - expectedBefore - expectedDelta) > tolerance)
                return NeedsReview(movement, "BASKET_WEIGHT_MISMATCH");
        }

        // كل الفحوص نجحت؛ الآن فقط نعدّل بنود الفاتورة.
        foreach (var change in changes)
            Apply(change, movement.DeviceBootId);
        // نضيف البنود الجديدة مرة واحدة إلى الحالة المستخدمة لحساب الإجمالي.
        invoiceItems.AddRange(newUnits);
        // قراءة الوزن أصبحت حالة سلة معتمدة بعد نجاح الحركة.
        if (movement.HasBasketChange)
            session.ReconciledBasketWeightG = movement.WeightAfterG;
        // لا يوجد انتظار أو إعادة تشغيل لهذه الحركة بعد قبولها.
        movement.Status = CartMovementStatus.Accepted;
        // نزيل سبب المراجعة لأن جميع الشروط نجحت.
        movement.ReasonCode = null;
        // الـRepository سيعمل INSERT لهذه البنود مع الحركة والإيصال في نفس الحفظ.
        return newUnits;
    }

    // وزن البند يؤثر في السلة فقط عند الدخول أو الخروج الفيزيائي.
    private static long PhysicalWeight(ItemChange change)
    {
        // الدخول الجديد أو تأكيد دخول قطعة ممسوحة يزيد الوزن المتوقع.
        if (change.Observation.Action == CartItemAction.Enter && change.Effect is CartItemEffect.Added or CartItemEffect.PlacementConfirmed)
            return change.Unit!.ExpectedWeightG!.Value;
        // الخروج يطرح وزن النسخة المحفوظة للقطعة المشتراة.
        if (change.Observation.Action == CartItemAction.Exit && change.Effect == CartItemEffect.Removed)
            return -change.Unit!.ExpectedWeightG!.Value;
        // السكان، التأكيد، والإلغاء خارج السلة لا يضيفون وزنًا.
        return 0;
    }

    // التنفيذ الفعلي يتم بعد اكتمال التحقق، وبعيدًا عن التخطيط السابق.
    private static void Apply(ItemChange change, Guid bootId)
    {
        // نثبت أن الملاحظة اجتازت فحوص الحركة كلها.
        change.Observation.Outcome = CartItemOutcome.Accepted;
        // نسجل أثرها المالي/الفيزيائي للعميل والمشرف.
        change.Observation.Effect = change.Effect;
        // قد يكون NoChange لقطعة دخلت وخرجت بدون بند مالي.
        if (change.Unit is null)
            return;
        // نربط النتيجة ببند الفاتورة الذي اختاره أو أنشأه السيرفر.
        change.Observation.SessionItemId = change.Unit.Id;
        // نرجع هوية الصنف وسعره ووزنه المعتمدين من النسخة المحفوظة.
        change.Observation.VariantId = change.Unit.VariantId;
        // سعر الفاتورة الأصلي له الأولوية على سعر الكتالوج الحالي.
        change.Observation.UnitPriceMinor = change.Unit.UnitPriceMinor;
        // نفس وزن الشراء يستخدم عند الخروج لضمان استقرار الحساب.
        change.Observation.ExpectedWeightG = change.Unit.ExpectedWeightG;
        // الدخول يسجل التتبع المؤكد للقطعة داخل السلة.
        if (change.Observation.Action == CartItemAction.Enter && change.Effect is CartItemEffect.Added or CartItemEffect.PlacementConfirmed)
        {
            // حالة Inside هي التي تدخل في حساب وزن السلة.
            change.Unit.BasketPresence = BasketPresence.Inside;
            // التتبع مرتبط بالكاميرا وتشغيل الجهاز لمنع خلط IDs بعد إعادة التشغيل.
            change.Unit.BasketTrackId = change.Observation.TrackId;
            // رقم التتبع قد يتكرر بين الكاميرات، لذلك نحفظ مصدره.
            change.Unit.BasketCameraId = change.Observation.CameraId;
            // إعادة تشغيل الجهاز لا تعني أن نفس TrackId يخص نفس القطعة.
            change.Unit.BasketDeviceBootId = bootId;
        }
        // الحذف يحافظ على السجل التاريخي بدل حذف الصف من قاعدة البيانات.
        if (change.Effect == CartItemEffect.Removed)
        {
            // RemovedAt يستبعد هذه الوحدة من الفاتورة النشطة.
            change.Unit.RemovedAt = DateTime.UtcNow;
            // القطعة خرجت فلا تدخل في وزن السلة المتوقع.
            change.Unit.BasketPresence = BasketPresence.Outside;
            // الطلب الموجود على الشاشة اكتمل.
            change.Unit.RemovalRequestedAt = null;
        }
    }

    // يحدد الصنف من السيرفر ويتحقق من ثقة الموديل بدون تغيير الفاتورة.
    private void Identify(CartMovementItem item, Dictionary<string, CartVariantData> catalog)
    {
        // ملاحظة بلا باركود تظل ظاهرة للمشرف ولا تتحول لصنف بالتخمين.
        if (string.IsNullOrWhiteSpace(item.DetectedBarcode)) item.ReasonCode = "ITEM_NOT_IDENTIFIED";
        // نتيجة الكاميرا تحتاج الحد الأدنى للثقة؛ السكان لا يعتمد على ثقة YOLO.
        else if (item.Source == CartItemSource.Vision && (!item.Confidence.HasValue || item.Confidence < options.Value.MinimumVisionConfidence))
            item.ReasonCode = "LOW_CONFIDENCE";
        // باركود غير معروف لا يضيف سعرًا أو صنفًا وهميًا.
        else if (!catalog.TryGetValue(item.DetectedBarcode, out var variant)) item.ReasonCode = "UNKNOWN_BARCODE";
        else
        {
            // بيانات مرشحة للعرض والمراجعة، ولا تعني أن الفاتورة اتغيرت.
            item.VariantId = variant.Id;
            // الوزن يأتي من الكتالوج ولا يثق في وزن يحدده العميل لكل صنف.
            item.ExpectedWeightG = variant.WeightG;
            // السعر أيضًا من السيرفر، وليس من JSON القادم من العربة.
            item.UnitPriceMinor = variant.PriceMinor;
        }
    }

    // السماحية لا تتجاوز إعداد السيرفر أو طلب العميل أو ربع أصغر قطعة.
    public int Tolerance(IEnumerable<int?> weights, int requested = 100)
    {
        // هذا الحد يمنع اختفاء قطعة صغيرة بالكامل داخل سماحية الميزان.
        var smallest = weights.Where(weight => weight > 0).Select(weight => weight!.Value).DefaultIfEmpty(int.MaxValue).Min();
        // نختار أشد حدود السماحية المتاحة.
        return Math.Min(Math.Min(requested, options.Value.WeightToleranceGrams), smallest / 4);
    }

    // السيرفر ينشئ وحدة فاتورة لكل قطعة مقبولة، حتى لو تكرر نفس الباركود.
    private static SessionItem NewUnit(Guid sessionId, CartMovementItem observation, CartVariantData variant, BasketPresence presence) => new()
    {
        // لا يأتي أي من هذين المعرفين من الجهاز كـID جديد لقطعة.
        Id = Guid.NewGuid(), SessionId = sessionId, VariantId = variant.Id,
        // نحفظ السعر والوزن والباركود وقت الإضافة.
        UnitPriceMinor = variant.PriceMinor, ExpectedWeightG = variant.WeightG, Barcode = variant.Barcode,
        // نسجل المصدر وحالة وجود القطعة داخل السلة.
        Source = observation.Source, BasketPresence = presence, AddedAt = DateTime.UtcNow
    };

    // الباركود المحفوظ يحمي إخراج مشتريات العميل بعد تغيير الكتالوج.
    private static bool Matches(SessionItem unit, CartMovementItem observation) =>
        unit.Barcode == observation.DetectedBarcode;

    // لا نختار بين أسعار أو أوزان مختلفة من غير مرجع واضح.
    private static SessionItem? SelectExit(CartMovementItem observation, CartMovement movement, List<SessionItem> candidates)
    {
        // اختيار الشاشة يشير لبند موجود أنشأه السيرفر سابقًا.
        if (observation.TargetSessionItemId.HasValue) return candidates.SingleOrDefault(unit => unit.Id == observation.TargetSessionItemId);
        // إذا كان التتبع مستمرًا في نفس تشغيل الجهاز يمكنه تحديد القطعة.
        var tracked = candidates.Where(unit => !string.IsNullOrWhiteSpace(observation.TrackId)
            && unit.BasketTrackId == observation.TrackId && unit.BasketCameraId == observation.CameraId
            && unit.BasketDeviceBootId == movement.DeviceBootId).ToList();
        // المطابقة الوحيدة تتجنب اختيار قطعة أخرى من نفس الصنف.
        if (tracked.Count == 1) return tracked[0];
        // الأسعار أو الأوزان المختلفة تحتاج قرار المستخدم/المشرف.
        if (candidates.Select(unit => (unit.VariantId, unit.UnitPriceMinor, unit.ExpectedWeightG)).Distinct().Count() > 1) return null;
        // القطع المتكافئة ماليًا: نفضل طلب الإزالة ثم الأقدم.
        return candidates.OrderByDescending(unit => unit.RemovalRequestedAt.HasValue).ThenBy(unit => unit.AddedAt).ThenBy(unit => unit.Id).FirstOrDefault();
    }

    // نفس التتبع في الاتجاهين يعني رجوع نفس القطعة داخل نافذة الحركة.
    private static List<CartMovementItem> FindRoundTrips(IEnumerable<CartMovementItem> items) => items
        // المرجع الصريح له أولوية على استنتاج التتبع.
        .Where(item => item.Action is CartItemAction.Enter or CartItemAction.Exit
            && !string.IsNullOrWhiteSpace(item.TrackId) && !item.TargetSessionItemId.HasValue)
        // الكاميرات قد تعيد استخدام نفس TrackId، لذلك نجمعه مع الكاميرا والباركود.
        .GroupBy(item => (item.TrackId, item.CameraId, item.DetectedBarcode))
        // نلغي أثر دورة واحدة واضحة، ولا نحاول تفسير تكرارات غامضة.
        .Where(group => group.Count() == 2 && group.Count(item => item.Action == CartItemAction.Enter) == 1
            && group.Count(item => item.Action == CartItemAction.Exit) == 1).SelectMany(group => group).ToList();

    // الفشل هنا نتيجة عمل تحتاج مشرفًا؛ لا يغيّر أي بند مالي.
    private static IReadOnlyList<SessionItem> NeedsReview(CartMovement movement, string reason)
    {
        // نحفظ السبب ليظهر في SessionUpdated.
        movement.ReasonCode = reason;
        // الحسم يدوي عبر ReconcileCartBasket، بدون تصحيحات تلقائية.
        movement.Status = CartMovementStatus.NeedsConfirmation;
        // لا توجد INSERTs جديدة للفواتير عند فشل الحركة.
        return [];
    }
}
