using SARE.Domain.Enums;

namespace SARE.Application.DTOs.Cart;

public record UpdateCartTelemetryRequest(
    short? BatteryPct,
    string? SwVersion,
    CartStatus? Status = null
);
