namespace SARE.Application.DTOs.Cart;

public record CartSummaryResponse(
    Guid SessionId,
    string CartId,
    Guid? UserId,
    string? UserName,
    string Status,
    int TotalMinor,
    int ItemsCount,
    DateTime StartedAt
);
