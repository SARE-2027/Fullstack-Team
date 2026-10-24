using Microsoft.AspNetCore.SignalR;
using SARE.Application.Common.Interfaces;
using SARE.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;
using SARE.Api.Authorization;
using SARE.Domain.Users;
using SARE.Application.DTOs.Cart;
using SARE.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace SARE.Api.Hubs;

public class CartHub(
    ICartRepository carts,
    ISessionRepository sessions,
    ISessionService sessionService,
    ICartService cartService,
    ICartMovementService movementService) : Hub
{
    private const string CartConnectionKey = "AuthenticatedCart";
    private sealed record CartConnection(string CartId, string HardwareToken);
    public const string SessionAdminsGroup = "SessionAdmins";

    public static string SessionCartGroup(string cartId) => $"session_cart_{cartId}";

    // First-time device provisioning is open for now; it does not join any groups.
    [AllowAnonymous]
    public Task<CartRegistrationResponse> RegisterCart(CreateCartRequest request) => ExecuteAsync(async () =>
    {
        if (request is null) throw new HubException("VALIDATION_FAILED: Request is required.");
        if (Context.Items.ContainsKey(CartConnectionKey))
            throw new HubException("CART_ALREADY_JOINED: This connection already has a cart identity.");

        return await cartService.RegisterCartAsync(request, Context.ConnectionAborted);
    });

    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public Task JoinSessionsGroup()
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, SessionAdminsGroup);
    }

    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public Task LeaveSessionsGroup()
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, SessionAdminsGroup);
    }

    public async Task<CartSummaryResponse?> JoinCartGroup(string cartId, string? hardwareToken = null)
    {
        if (string.IsNullOrWhiteSpace(cartId) || cartId.Length > 32)
            throw new HubException("INVALID_CART_ID: CartId is required and must not exceed 32 characters.");

        var cart = await carts.GetByIdAsync(cartId);
        if (cart is null)
        {
            throw new HubException($"العربة {cartId} غير مسجلة في النظام");
        }

        // Cart subscriptions require a provisioned hardware token, including for anonymous device clients.
        if (string.IsNullOrWhiteSpace(cart.TokenHash) || string.IsNullOrWhiteSpace(hardwareToken)
            || !TokenHasher.VerifyToken(hardwareToken, cart.TokenHash))
        {
            throw new HubException("رمز مصادقة العربة غير صالح للاتصال بالقناة");
        }

        if (Context.Items.TryGetValue(CartConnectionKey, out var previous) && previous is CartConnection connection)
            await LeaveCartGroup(connection.CartId);

        await Groups.AddToGroupAsync(Context.ConnectionId, $"cart_{cartId}");
        // Admins receive SessionUpdated through JoinSessionsGroup, avoiding overlapping recipients.
        if (Context.User?.IsInRole(UserRoles.Admin) != true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, SessionCartGroup(cartId));
        }

        // Context.Items survives hub method calls; hub instances themselves are transient.
        Context.Items[CartConnectionKey] = new CartConnection(cartId, hardwareToken);
        // Subscribe first, then read, so a change during synchronization is also delivered.
        return await sessions.GetActiveSummaryByCartIdAsync(cartId, Context.ConnectionAborted);
    }

    public async Task LeaveCartGroup(string cartId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"cart_{cartId}");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SessionCartGroup(cartId));
        if (Context.Items.TryGetValue(CartConnectionKey, out var value)
            && value is CartConnection connection && connection.CartId == cartId)
        {
            Context.Items.Remove(CartConnectionKey);
        }
    }

    public Task<CartSummaryResponse> StartSession(CartStartSessionRequest request) => ExecuteAsync(async () =>
    {
        var connection = await RequireCartAsync();
        if (request is null) throw new HubException("VALIDATION_FAILED: Request is required.");
        return await sessionService.StartSessionAsync(
            new StartSessionRequest(connection.CartId, request.NfcUid), Context.ConnectionAborted);
    });

    public Task<CartTelemetryResponse> UpdateCartTelemetry(UpdateCartTelemetryRequest request) => ExecuteAsync(async () =>
    {
        var connection = await RequireCartAsync();
        if (request is null) throw new HubException("VALIDATION_FAILED: Request is required.");
        return await sessionService.UpdateCartTelemetryAsync(
            connection.CartId, request, connection.HardwareToken, Context.ConnectionAborted);
    });

    public Task<CartMovementResult> ReportCartMovement(Guid sessionId, CartMovementRequest request) => ExecuteAsync(async () =>
    {
        var connection = await RequireCartAsync();
        await RequireCartSessionAsync(connection.CartId, sessionId);
        if (request is null) throw new HubException("VALIDATION_FAILED: Request is required.");
        return await movementService.ReportAsync(sessionId, request, Context.ConnectionAborted);
    });

    // القرار يخص تقرير خروج ونسخة سيشن محددين، وبنفس تحقق ملكية العربة.
    public Task<CartMovementResult> ConfirmExitChoice(Guid sessionId, ConfirmExitChoiceRequest request) => ExecuteAsync(async () =>
    {
        var connection = await RequireCartAsync();
        await RequireCartSessionAsync(connection.CartId, sessionId);
        if (request is null) throw new HubException("VALIDATION_FAILED: Request is required.");
        return await movementService.ConfirmExitAsync(sessionId, request, Context.ConnectionAborted);
    });

    public Task<CartSummaryResponse> RemoveSessionItem(Guid sessionId, Guid itemId) => ExecuteAsync(async () =>
    {
        var connection = await RequireCartAsync();
        await RequireCartSessionAsync(connection.CartId, sessionId);
        return await movementService.SetRemovalRequestAsync(sessionId, itemId, true, Context.ConnectionAborted);
    });

    public Task<CartSummaryResponse> CancelItemRemoval(Guid sessionId, Guid itemId) => ExecuteAsync(async () =>
    {
        var connection = await RequireCartAsync();
        await RequireCartSessionAsync(connection.CartId, sessionId);
        return await movementService.SetRemovalRequestAsync(sessionId, itemId, false, Context.ConnectionAborted);
    });

    public Task<CartMovementResult> RequestStaff(Guid sessionId, CartAuditCommand request) => ExecuteAsync(async () =>
    {
        var connection = await RequireCartAsync();
        await RequireCartSessionAsync(connection.CartId, sessionId);
        if (request is null) throw new HubException("VALIDATION_FAILED: Request is required.");
        return await movementService.RequestStaffAsync(sessionId, request, Context.ConnectionAborted);
    });

    public Task<CartSummaryResponse> CloseSession(Guid sessionId) => ExecuteAsync(async () =>
    {
        var connection = await RequireCartAsync();
        await RequireCartSessionAsync(connection.CartId, sessionId);
        // A device closes its session without impersonating an administrative user.
        return await sessionService.CloseSessionAsync(sessionId, ct: Context.ConnectionAborted);
    });

    private async Task<CartConnection> RequireCartAsync()
    {
        if (!Context.Items.TryGetValue(CartConnectionKey, out var value) || value is not CartConnection connection)
            throw new HubException("CART_NOT_JOINED: Call JoinCartGroup before sending cart commands.");

        var cart = await carts.GetByIdAsync(connection.CartId, Context.ConnectionAborted);
        if (cart is null || string.IsNullOrWhiteSpace(cart.TokenHash)
            || !TokenHasher.VerifyToken(connection.HardwareToken, cart.TokenHash))
        {
            throw new HubException("INVALID_CART_TOKEN: Cart credentials are no longer valid.");
        }
        return connection;
    }

    private async Task RequireCartSessionAsync(string cartId, Guid sessionId)
    {
        var session = await sessions.GetByIdAsync(sessionId, Context.ConnectionAborted);
        if (session is null || session.CartId != cartId)
            throw new HubException("SESSION_NOT_ACCESSIBLE: Session does not belong to this cart.");
    }

    private static async Task<T> ExecuteAsync<T>(Func<Task<T>> command)
    {
        try
        {
            return await command();
        }
        catch (AppException exception)
        {
            throw new HubException($"{exception.ErrorCode}: {exception.Message}");
        }
        catch (FluentValidation.ValidationException exception)
        {
            throw new HubException($"VALIDATION_FAILED: {string.Join("; ", exception.Errors.Select(error => error.ErrorMessage))}");
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new HubException("CONFLICT: Session changed concurrently. Synchronize and retry with the same eventId.");
        }
    }
}
