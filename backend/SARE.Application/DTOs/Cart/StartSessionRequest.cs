namespace SARE.Application.DTOs.Cart;

public record StartSessionRequest(
    string CartId,
    string? NfcUid
);
