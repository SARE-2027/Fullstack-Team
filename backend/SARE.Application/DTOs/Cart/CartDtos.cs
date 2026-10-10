using SARE.Domain.Enums;

namespace SARE.Application.DTOs.Cart;

public record StartSessionRequest(
    string CartId,
    Guid? UserId
);

public record CartAddItemRequest(
    string Barcode,
    int MeasuredWeightG,
    DetectionSource Source = DetectionSource.Scanner
);

public record SessionItemDto(
    Guid Id,
    Guid VariantId,
    string ProductName,
    string Barcode,
    int UnitPriceMinor,
    int ExpectedWeightG,
    int Quantity,
    DateTime AddedAt
);

public record CartSessionResponseDto(
    Guid SessionId,
    string CartId,
    SessionStatus Status,
    int TotalMinor,
    DateTime StartedAt,
    List<SessionItemDto> Items
);

public record CartItemProcessResult(
    bool Success,
    string Message,
    bool IsWeightMismatch = false,
    int? ExpectedWeightG = null,
    int? MeasuredWeightG = null,
    CartSessionResponseDto? Session = null
);

public record CheckoutResponseDto(
    Guid SessionId,
    string CartId,
    int TotalPaidMinor,
    string ReceiptHash,
    DateTime ClosedAt
);
