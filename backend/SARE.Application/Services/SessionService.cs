using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;
using SARE.Domain.Enums;
using SARE.Domain.Users;

namespace SARE.Application.Services;

public class SessionService(
    ICartRepository carts,
    ISessionRepository sessions,
    IUserRepository users) : ISessionService
{
    public async Task UpdateCartTelemetryAsync(
        string cartId,
        UpdateCartTelemetryRequest request,
        CancellationToken ct = default)
    {
        var cart = await carts.GetByIdAsync(cartId, ct)
            ?? throw new KeyNotFoundException($"العربة {cartId} غير موجودة");

        if (request.BatteryPct is not null)
            cart.BatteryPct = request.BatteryPct;

        if (request.SwVersion is not null)
            cart.SwVersion = request.SwVersion;

        cart.LastSeenAt = DateTime.UtcNow;

        await carts.UpdateAsync(cart, ct);
    }

    public async Task<CartSummaryResponse> StartSessionAsync(
        StartSessionRequest request,
        CancellationToken ct = default)
    {
        // 1. فحص وجود السلة وحالتها
        var cart = await carts.GetByIdAsync(request.CartId, ct)
            ?? throw new KeyNotFoundException($"العربة {request.CartId} غير موجودة");

        if (cart.Status == CartStatus.Disabled)
            throw new InvalidOperationException($"العربة {request.CartId} معطلة حالياً");

        // 2. التأكد من عدم وجود جلسة نشطة على هذه السلة
        var activeSession = await sessions.GetActiveByCartIdAsync(request.CartId, ct);
        if (activeSession is not null)
            throw new InvalidOperationException($"العربة {request.CartId} لديها جلسة نشطة بالفعل");

        // 3. فحص كارت الـ NFC (إن وُجد) والوصول للمستخدم
        User? user = null;
        if (!string.IsNullOrWhiteSpace(request.NfcUid))
        {
            user = await users.GetByNfcUidAsync(request.NfcUid, ct)
                ?? throw new KeyNotFoundException($"كارت الـ NFC ({request.NfcUid}) غير مسجل لأي مستخدم");

            if (!user.IsActive)
                throw new InvalidOperationException("حساب المستخدم المرتبط بهذا الكارت معطل");
        }

        // 4. إنشاء الجلسة الجديدة
        var now = DateTime.UtcNow;
        var session = new Session
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            UserId = user?.Id,
            Status = SessionStatus.Open,
            TotalMinor = 0,
            StartedAt = now,
            LastActivityAt = now
        };

        await sessions.AddAsync(session, ct);

        // 5. تحديث آخر ظهور للعربة
        cart.LastSeenAt = now;
        await carts.UpdateAsync(cart, ct);

        // 6. إرجاع ملخص الجلسة
        return new CartSummaryResponse(
            SessionId: session.Id,
            CartId: session.CartId,
            UserId: user?.Id,
            UserName: user?.Name,
            Status: session.Status.ToString().ToLowerInvariant(),
            TotalMinor: session.TotalMinor,
            ItemsCount: 0,
            StartedAt: session.StartedAt,
            Items: []
        );
    }

    public async Task<CartSummaryResponse> GetSessionSummaryAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        var (session, items) = await sessions.GetWithItemsAsync(sessionId, ct);
        if (session is null)
            throw new KeyNotFoundException($"الجلسة {sessionId} غير موجودة");

        string? userName = null;
        if (session.UserId.HasValue)
        {
            var user = await users.GetByIdAsync(session.UserId.Value, ct);
            userName = user?.Name;
        }

        return new CartSummaryResponse(
            SessionId: session.Id,
            CartId: session.CartId,
            UserId: session.UserId,
            UserName: userName,
            Status: session.Status.ToString().ToLowerInvariant(),
            TotalMinor: session.TotalMinor,
            ItemsCount: items.Count,
            StartedAt: session.StartedAt,
            Items: items
        );
    }

    public async Task<CartSummaryResponse> GetActiveSessionByCartIdAsync(
        string cartId,
        CancellationToken ct = default)
    {
        var cart = await carts.GetByIdAsync(cartId, ct)
            ?? throw new KeyNotFoundException($"العربة {cartId} غير موجودة");

        var (session, items) = await sessions.GetActiveWithItemsByCartIdAsync(cartId, ct);
        if (session is null)
            throw new KeyNotFoundException($"لا توجد جلسة نشطة للعربة {cartId} حالياً");

        string? userName = null;
        if (session.UserId.HasValue)
        {
            var user = await users.GetByIdAsync(session.UserId.Value, ct);
            userName = user?.Name;
        }

        return new CartSummaryResponse(
            SessionId: session.Id,
            CartId: session.CartId,
            UserId: session.UserId,
            UserName: userName,
            Status: session.Status.ToString().ToLowerInvariant(),
            TotalMinor: session.TotalMinor,
            ItemsCount: items.Count,
            StartedAt: session.StartedAt,
            Items: items
        );
    }

    public async Task<CartSummaryResponse> CloseSessionAsync(
        Guid sessionId,
        CloseSessionRequest? request = null,
        CancellationToken ct = default)
    {
        // 1. جلب الجلسة مع أصنافها
        var (session, items) = await sessions.GetWithItemsAsync(sessionId, ct);
        if (session is null)
            throw new KeyNotFoundException($"الجلسة {sessionId} غير موجودة");

        // 2. التحقق من أن الجلسة ما زالت مفتوحة
        if (session.Status != SessionStatus.Open)
            throw new InvalidOperationException($"الجلسة {sessionId} مغلقة بالفعل بحالة ({session.Status})");

        // 3. التحقق من وجود الموظف المغلق (إن وجد)
        if (request?.ClosedByUserId.HasValue == true)
        {
            var closer = await users.GetByIdAsync(request.ClosedByUserId.Value, ct);
            if (closer is null)
                throw new KeyNotFoundException($"الموظف {request.ClosedByUserId.Value} غير موجود في النظام");
        }

        // 4. تحديث حالة الجلسة ووقت الإغلاق
        var now = DateTime.UtcNow;
        session.Status = request?.Reason == CloseReason.Abandoned
            ? SessionStatus.Abandoned
            : SessionStatus.Closed;

        session.ClosedAt = now;
        session.LastActivityAt = now;
        session.ClosedBy = request?.ClosedByUserId;
        session.CloseReason = request?.Reason;

        await sessions.UpdateAsync(session, ct);

        // 5. جلب اسم المتسوق للعرض في الفاتورة
        string? userName = null;
        if (session.UserId.HasValue)
        {
            var user = await users.GetByIdAsync(session.UserId.Value, ct);
            userName = user?.Name;
        }

        // 6. إرجاع ملخص الفاتورة المغلقة
        return new CartSummaryResponse(
            SessionId: session.Id,
            CartId: session.CartId,
            UserId: session.UserId,
            UserName: userName,
            Status: session.Status.ToString().ToLowerInvariant(),
            TotalMinor: session.TotalMinor,
            ItemsCount: items.Count,
            StartedAt: session.StartedAt,
            Items: items
        );
    }
}