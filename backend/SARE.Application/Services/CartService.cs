using System.Security.Cryptography;
using FluentValidation;
using SARE.Application.Common.Interfaces;
using SARE.Application.Common.Security;
using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;
using SARE.Domain.Enums;

namespace SARE.Application.Services;

public class CartService(
    ICartNotificationService notifications,
    ISessionService sessionService,
    ICartRepository cartRepository,
    IValidator<CreateCartRequest> createCartValidator) : ICartService
{
    public async Task<CartRegistrationResponse> RegisterCartAsync(CreateCartRequest request, CancellationToken cancellationToken = default)
    {
        var swVersion = request.SwVersion?.Trim();
        request = request with { SwVersion = string.IsNullOrEmpty(swVersion) ? null : swVersion };
        await createCartValidator.ValidateAndThrowAsync(request, cancellationToken);

        var hardwareToken = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var cart = new Cart
        {
            Id = Guid.NewGuid().ToString("N"),
            TokenHash = TokenHasher.HashToken(hardwareToken),
            Status = CartStatus.Active,
            SwVersion = request.SwVersion
        };

        await cartRepository.AddAsync(cart, cancellationToken);

        return new CartRegistrationResponse(cart.Id, hardwareToken, "active", cart.SwVersion);
    }

    public async Task<CheckoutResponseDto> CheckoutSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var summary = await sessionService.CloseSessionAsync(sessionId, ct: cancellationToken);
        var closedAt = summary.ClosedAt!.Value;
        var receiptHash = $"RECEIPT_{summary.SessionId}_{closedAt.Ticks}";
        await notifications.NotifyCheckoutSuccessAsync(summary.CartId, summary.SessionId, summary.TotalMinor, receiptHash);
        return new CheckoutResponseDto(summary.SessionId, summary.CartId, summary.TotalMinor, receiptHash, closedAt);
    }
}
