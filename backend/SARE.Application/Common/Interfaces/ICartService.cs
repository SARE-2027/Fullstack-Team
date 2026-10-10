using SARE.Application.DTOs.Cart;

namespace SARE.Application.Common.Interfaces;

public interface ICartService
{
    Task<CartSessionResponseDto> StartSessionAsync(StartSessionRequest request, CancellationToken cancellationToken = default);
    Task<CartItemProcessResult> ProcessItemDetectionAsync(Guid sessionId, CartAddItemRequest request, CancellationToken cancellationToken = default);
    Task<CheckoutResponseDto> CheckoutSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<CartSessionResponseDto?> GetActiveSessionByCartIdAsync(string cartId, CancellationToken cancellationToken = default);
    Task<CartSessionResponseDto?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
