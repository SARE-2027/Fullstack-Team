using SARE.Domain.Enums;

namespace SARE.Application.DTOs.Cart;

public record CloseSessionRequest(
    Guid? ClosedByUserId = null,
    CloseReason? Reason = null
);
