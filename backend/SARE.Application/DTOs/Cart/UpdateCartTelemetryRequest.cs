namespace SARE.Application.DTOs.Cart;

public record UpdateCartTelemetryRequest(
    short? BatteryPct,
    string? SwVersion
);
