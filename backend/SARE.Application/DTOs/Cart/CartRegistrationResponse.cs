namespace SARE.Application.DTOs.Cart;

// The raw hardware token is returned only when provisioning the device.
public record CartRegistrationResponse(
    string CartId,
    string HardwareToken,
    string Status,
    string? SwVersion);
