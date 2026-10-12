using SARE.Domain.Enums;

namespace SARE.Application.DTOs.Cart;

public record LegacyStartSessionRequest(
    string CartId,
    Guid? UserId
);

public record CartAddItemRequest(
    string Barcode,
    int MeasuredWeightG,
    DetectionSource Source = DetectionSource.Scanner,
    Guid? EventId = null
);

public record CartItemProcessResult(
    bool Success,
    string Message,
    bool IsWeightMismatch = false,
    int? ExpectedWeightG = null,
    int? MeasuredWeightG = null,
    CartSummaryResponse? Session = null
);

public record CheckoutResponseDto(
    Guid SessionId,
    string CartId,
    int TotalPaidMinor,
    string ReceiptHash,
    DateTime ClosedAt
);
