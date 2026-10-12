using SARE.Application.DTOs.Cart;

namespace SARE.Application.Common.Interfaces;

public interface ICartService
{
    Task<CartItemProcessResult> ProcessItemDetectionAsync(Guid sessionId, CartAddItemRequest request, CancellationToken cancellationToken = default);
    Task<CheckoutResponseDto> CheckoutSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
