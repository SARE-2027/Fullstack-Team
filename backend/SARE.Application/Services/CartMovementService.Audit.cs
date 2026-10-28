using System.Globalization;
using SARE.Application.Common.Exceptions;
using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;
using SARE.Domain.Enums;

namespace SARE.Application.Services;

// نفس خدمة الحركة والحفظ؛ هذا الملف يجمع فقط خطوات المراجعة البشرية.
public sealed partial class CartMovementService
{
    public async Task<CartMovementResult> RequestStaffAsync(Guid sessionId, CartAuditCommand request, CancellationToken ct = default)
    {
        ValidateAuditCommand(request);
        var hash = Hash(new { Action = "RequestStaff", Request = request });
        var receipt = await repository.GetReceiptAsync(request.EventId, ct);
        if (receipt is not null) return await ReplayAsync(receipt, sessionId, request.MovementId, hash, ct);
        var session = await RequireAuditCommandSessionAsync(sessionId, request, ct);
        if (session.BasketState == BasketState.Auditing)
            throw new ConflictException("An audit is already active.", "AUDIT_ACTIVE");

        session.BasketState = BasketState.NeedsStaff;
        session.StaffRequestedAt ??= DateTime.UtcNow;
        session.StaffRequestReason = request.Reason ?? "CUSTOMER_REQUESTED_HELP";
        var decision = AuditDecision(session, request, "STAFF_REQUESTED");
        return await SaveAndPublishAsync(session, decision,
            Receipt(sessionId, decision.Id, request.EventId, request.Sequence, request.DeviceBootId, hash), [], ct);
    }

    public async Task<CartMovementResult> ManageAuditAsync(Guid sessionId, CartAuditAction action, Guid adminUserId,
        CartAuditCommand request, CancellationToken ct = default)
    {
        ValidateAuditCommand(request);
        // الصلاحية تتحقق في الـpolicy؛ نحتاج هنا هوية فعلية لتوثيق المسؤول عن الجرد.
        if (adminUserId == Guid.Empty)
            throw new UnauthorizedException("An identifiable user is required to record the audit actor.");
        if (!Enum.IsDefined(action))
            throw new ConflictException("A valid audit action is required.", "VALIDATION_FAILED");
        // هوية الأدمن مأخوذة من JWT في الـAPI، وليست قيمة يرسلها صاحب العربية.
        var hash = Hash(new { Action = action, AdminUserId = adminUserId, Request = request });
        var receipt = await repository.GetReceiptAsync(request.EventId, ct);
        if (receipt is not null) return await ReplayAsync(receipt, sessionId, request.MovementId, hash, ct);
        var session = await RequireAuditCommandSessionAsync(sessionId, request, ct);
        var audit = await repository.GetAuditAsync(sessionId, ct);
        var decision = AuditDecision(session, request, $"AUDIT_{action.ToString().ToUpperInvariant()}");
        IReadOnlyList<SessionItem> additions = [];

        if (action == CartAuditAction.Start)
        {
            if (session.BasketState != BasketState.NeedsStaff || audit is not null)
                throw new ConflictException("Request staff before starting a new audit.", "AUDIT_NOT_AVAILABLE");
            audit = new CartBasketAudit
            {
                Id = Guid.NewGuid(), SessionId = sessionId,
                StartedByUserId = adminUserId, StartedAtUtc = Microseconds(DateTime.UtcNow)
            };
            repository.AddAudit(audit);
            session.BasketState = BasketState.Auditing;
        }
        else
        {
            if (audit is null || session.BasketState != BasketState.Auditing || request.AuditId != audit.Id)
                throw new ConflictException("Refresh the active audit.", "AUDIT_CHANGED");

            switch (action)
            {
                case CartAuditAction.RemoveItem:
                    var draft = audit.Items.SingleOrDefault(item => item.Id == request.AuditItemId && !item.IsRemoved)
                        ?? throw new NotFoundException("Audit item not found.");
                    draft.IsRemoved = true;
                    // تصحيح القائمة يحتاج قراءة وزن جديدة قبل الاعتماد.
                    audit.LastScanSequence = request.Sequence;
                    break;
                case CartAuditAction.Commit:
                    additions = await CommitAuditAsync(session, audit, decision, adminUserId, ct);
                    break;
            }
        }

        decision.AuditId = audit.Id;
        return await SaveAndPublishAsync(session, decision,
            Receipt(sessionId, decision.Id, request.EventId, request.Sequence, request.DeviceBootId, hash), additions, ct);
    }

    private async Task<IReadOnlyList<SessionItem>> CommitAuditAsync(Session session, CartBasketAudit audit,
        CartMovement decision, Guid adminUserId, CancellationToken ct)
    {
        RequireRecentAuditWeight(audit);
        if (audit.LastWeightSequence < audit.LastScanSequence)
            throw new ConflictException("Send a final stable weight after all scans/corrections.", "AUDIT_WEIGHT_REQUIRED");
        var counted = audit.Items.Where(item => !item.IsRemoved).ToList();
        if (counted.Any(item => item.ScannedAtUtc > audit.LastWeightAtUtc))
            throw new ConflictException("The final weight must be captured after the audit scans.", "AUDIT_WEIGHT_REQUIRED");
        var range = CartWeightPolicy.Sum(counted.Select(item =>
            CartWeightRange.For(item.ExpectedWeightG, item.WeightTolerancePercent)));
        if (!range.Contains(audit.LastStableWeightG!.Value))
            throw new ConflictException("Audit contents and basket weight do not agree.", "AUDIT_WEIGHT_MISMATCH");
        var total = counted.Sum(item => (long)item.UnitPriceMinor);
        if (total > int.MaxValue) throw new ConflictException("Audit total exceeds the supported amount.", "TOTAL_LIMIT_EXCEEDED");

        // القائمة المؤقتة تصبح الفاتورة مرة واحدة؛ القديمة تظل محفوظة كتاريخ إزالة.
        var previous = await repository.GetSessionItemsAsync(session.Id, ct);
        foreach (var item in previous.Where(item => item.RemovedAt == null)) CartMovementProcessor.RemoveUnit(item);
        var additions = counted.Select(item => new SessionItem
        {
            Id = Guid.NewGuid(), SessionId = session.Id, VariantId = item.VariantId, Barcode = item.Barcode,
            UnitPriceMinor = item.UnitPriceMinor, ExpectedWeightG = item.ExpectedWeightG,
            WeightTolerancePercent = item.WeightTolerancePercent,
            Source = CartItemSource.Manual, BasketPresence = BasketPresence.Inside,
            PlacementSequence = decision.Sequence, AddedAt = DateTime.UtcNow
        }).ToList();
        foreach (var item in additions)
            decision.Items.Add(new CartMovementItem
            {
                Id = Guid.NewGuid(), MovementId = decision.Id, Position = decision.Items.Count,
                Action = CartItemAction.Enter, Source = CartItemSource.Manual,
                Outcome = CartItemOutcome.Accepted, Effect = CartItemEffect.Added,
                SessionItemId = item.Id, VariantId = item.VariantId, DetectedBarcode = item.Barcode,
                UnitPriceMinor = item.UnitPriceMinor, ExpectedWeightG = item.ExpectedWeightG,
                WeightTolerancePercent = item.WeightTolerancePercent
            });
        foreach (var pending in await repository.GetUnresolvedAsync(session.Id, ct))
        {
            CancelUnresolved(pending, "RESOLVED_BY_AUDIT");
            pending.ResolvedByMovementId = decision.Id;
        }

        audit.CompletedAtUtc = session.LastAuditCompletedAtUtc = Microseconds(DateTime.UtcNow);
        audit.CompletedByUserId = adminUserId;
        session.TotalMinor = (int)total;
        session.PendingWeightG = session.PendingWeightUncertaintyG = 0;
        session.BasketWeightG = audit.LastStableWeightG;
        session.StaffRequestedAt = null;
        session.StaffRequestReason = null;
        session.BasketState = BasketState.Verified;
        return additions;
    }

    private async Task<CartMovementResult> ReportReviewAsync(Session session, CartMovementRequest request, string hash, CancellationToken ct)
    {
        if (await repository.GetAsync(request.MovementId, ct) is not null)
            throw new ConflictException("Retry the original eventId.", "MOVEMENT_ALREADY_RECORDED");
        var movement = CreateMovement(session.Id, request);
        movement.PendingWeightBeforeG = session.PendingWeightG;
        movement.Status = CartMovementStatus.Accepted;
        movement.ReasonCode = "REVIEW_TELEMETRY_ONLY";
        var audit = session.BasketState == BasketState.Auditing ? await repository.GetAuditAsync(session.Id, ct) : null;
        movement.AuditId = request.AuditId ?? audit?.Id;
        if (audit is null && session.BasketState is (BasketState.Verified or BasketState.Pending)
            && (!session.LastAuditCompletedAtUtc.HasValue || request.EndedAtUtc > session.LastAuditCompletedAtUtc.Value))
        {
            // التقرير الممتد لما بعد الاعتماد يحتاج مراجعة؛ لا نسقط مشتريات جديدة باعتبارها تسجيلًا قديمًا.
            session.BasketState = BasketState.NeedsStaff;
            session.StaffRequestReason = movement.ReasonCode = request.AuditId.HasValue
                ? "STALE_AUDIT_CONTEXT" : "AUDIT_BOUNDARY_MOVEMENT";
        }
        if (session.BasketState == BasketState.NeedsStaff && request.Weight is not null)
            session.BasketWeightG = request.Weight.AfterG;
        // معرف الجرد وحده لا يكفي بعد انتهائه؛ نراجع حالته ووقت التقاط الرسالة أيضًا.
        foreach (var observation in movement.Items)
        {
            observation.Outcome = CartItemOutcome.Accepted;
            observation.Effect = CartItemEffect.NoChange;
        }

        if (audit is not null && request.AuditId == audit.Id && request.StartedAtUtc >= audit.StartedAtUtc)
        {
            if (request.Weight is not null)
            {
                audit.LastStableWeightG = request.Weight.AfterG;
                audit.LastWeightSequence = request.Sequence;
                audit.LastWeightAtUtc = request.EndedAtUtc;
                session.BasketWeightG = request.Weight.AfterG;
            }
            var scans = movement.Items.Where(item => item.Action == CartItemAction.Scan).ToList();
            if (scans.Count > 0)
            {
                // Start يفتح المسح مباشرة؛ تفريغ السلة إجراء يقوم به الموظف دون مرحلة تأكيد منفصلة.
                var catalog = await variants.GetCartVariantsAsync(scans.Select(item => item.DetectedBarcode!).Distinct().ToArray(), ct);
                var previous = await repository.GetSessionItemsAsync(session.Id, ct);
                foreach (var scan in scans)
                {
                    if (!catalog.TryGetValue(scan.DetectedBarcode!, out var variant))
                    {
                        scan.Outcome = CartItemOutcome.Rejected;
                        scan.ReasonCode = "UNKNOWN_BARCODE";
                        continue;
                    }
                    if (audit.Items.Count(item => !item.IsRemoved) >= 500)
                        throw new ConflictException("An audit supports at most 500 pieces.", "AUDIT_ITEM_LIMIT");
                    // القطع التي سبق شراؤها تحتفظ بسعرها؛ القطع الزائدة تأخذ السعر الحالي.
                    var original = previous.Where(item => item.RemovedAt == null && item.VariantId == variant.Id
                            && !audit.Items.Any(draft => !draft.IsRemoved && draft.OriginalSessionItemId == item.Id))
                        .OrderBy(item => item.AddedAt).ThenBy(item => item.Id).FirstOrDefault();
                    var draft = new CartBasketAuditItem
                    {
                        Id = Guid.NewGuid(), AuditId = audit.Id, VariantId = variant.Id, Barcode = variant.Barcode,
                        OriginalSessionItemId = original?.Id, UnitPriceMinor = original?.UnitPriceMinor ?? variant.PriceMinor,
                        ExpectedWeightG = original?.ExpectedWeightG ?? variant.WeightG,
                        WeightTolerancePercent = original?.ExpectedWeightG.HasValue == true
                            ? original.WeightTolerancePercent : processor.ResolveTolerancePercent(variant.WeightTolerancePercent),
                        ScannedAtUtc = request.EndedAtUtc
                    };
                    audit.Items.Add(draft);
                    scan.VariantId = draft.VariantId;
                    scan.ExpectedWeightG = draft.ExpectedWeightG;
                    scan.WeightTolerancePercent = draft.WeightTolerancePercent;
                    scan.UnitPriceMinor = draft.UnitPriceMinor;
                    scan.ReasonCode = "AUDIT_SCAN_RECORDED";
                }
                audit.LastScanSequence = request.Sequence;
            }
            movement.ReasonCode = "AUDIT_RECORDED";
        }
        else if (request.Items.Any(item => item.Action == CartItemAction.Scan))
        {
            // سكان بلا معرف الجرد قد يكون رسالة تسوق مؤجلة؛ لا نضيفه لأي قائمة.
            foreach (var scan in movement.Items.Where(item => item.Action == CartItemAction.Scan))
            {
                scan.Outcome = CartItemOutcome.Rejected;
                scan.ReasonCode = "STAFF_REVIEW_ACTIVE";
            }
        }
        session.LastMovementSequence = request.Sequence;
        return await SaveAndPublishAsync(session, movement,
            Receipt(session.Id, movement.Id, request.EventId, request.Sequence, request.DeviceBootId, hash), [], ct);
    }

    private static void RequireRecentAuditWeight(CartBasketAudit audit)
    {
        if (!audit.LastStableWeightG.HasValue || !audit.LastWeightAtUtc.HasValue
            || DateTime.UtcNow - audit.LastWeightAtUtc.Value > TimeSpan.FromSeconds(30))
            throw new ConflictException("Send a stable audit weight from the cart within the last 30 seconds.", "AUDIT_WEIGHT_REQUIRED");
    }

    private async Task<Session> RequireAuditCommandSessionAsync(Guid sessionId, CartAuditCommand request, CancellationToken ct)
    {
        var session = await RequireOpenSessionAsync(sessionId, request.Sequence, ct);
        if (session.LastActivityAt.Ticks.ToString(CultureInfo.InvariantCulture) != request.ExpectedVersion)
            throw new ConflictException("Refresh the session before this decision.", "SESSION_VERSION_CHANGED");
        if (await repository.GetAsync(request.MovementId, ct) is not null)
            throw new ConflictException("Retry the original eventId.", "MOVEMENT_ALREADY_RECORDED");
        return session;
    }

    private static void ValidateAuditCommand(CartAuditCommand request)
    {
        if (request.EventId == Guid.Empty || request.MovementId == Guid.Empty || request.DeviceBootId == Guid.Empty
            || request.Sequence is < 1 or >= int.MaxValue || string.IsNullOrWhiteSpace(request.ExpectedVersion)
            || request.Reason?.Length > 200)
            throw new ConflictException("Valid command metadata and current session version are required.", "VALIDATION_FAILED");
    }

    private static CartMovement AuditDecision(Session session, CartAuditCommand request, string reason)
    {
        session.LastMovementSequence = request.Sequence;
        var now = Microseconds(DateTime.UtcNow);
        return new CartMovement
        {
            Id = request.MovementId, SessionId = session.Id, Sequence = request.Sequence, DeviceBootId = request.DeviceBootId,
            StartedAtUtc = now, EndedAtUtc = now, Status = CartMovementStatus.Accepted,
            PendingWeightBeforeG = session.PendingWeightG, ReasonCode = reason
        };
    }
}
