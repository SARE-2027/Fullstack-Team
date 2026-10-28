namespace SARE.Application.DTOs.Cart;

public record LegacyStartSessionRequest(
    string CartId,
    Guid? UserId
);

public record CheckoutResponseDto(
    Guid SessionId,
    string CartId,
    int TotalPaidMinor,
    string ReceiptHash,
    DateTime ClosedAt
);
