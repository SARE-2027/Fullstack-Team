namespace SARE.Application.DTOs.Cart;

public record CartTelemetryResponse(
    string CartId,
    string Status,
    short? BatteryPct,
    int NextReportIntervalSeconds,
    DateTime AcknowledgedAt
);
